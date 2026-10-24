import { useSyncExternalStore } from 'react'

/**
 * The kids area's sounds, made in code with the Web Audio API (owner 2026-10-24: no sound files to license): short
 * effects, the sea and a music-box tune. Browsers keep sound off until the first tap, so `unlockSound` runs on every
 * tap in the kids area. The child (or a Parent) can turn sound off; the choice is kept on the device.
 */
export type SoundEffect =
  'tap' | 'step' | 'splash' | 'correct' | 'wrong' | 'hit' | 'star' | 'win' | 'whoosh' | 'rustle' | 'reveal'

const storageKey = 'kids.sound'
const listeners = new Set<() => void>()

let context: AudioContext | null = null
let master: GainNode | null = null
let noise: AudioBuffer | null = null

function readMuted() {
  try {
    return localStorage.getItem(storageKey) === 'off'
  } catch {
    return false
  }
}

export function setMuted(muted: boolean) {
  try {
    localStorage.setItem(storageKey, muted ? 'off' : 'on')
  } catch {
    // Without storage the choice lasts until the page closes.
  }
  if (master) master.gain.value = muted ? 0 : 1
  listeners.forEach((listener) => listener())
}

/** Whether sound is off, re-rendering when it changes. */
export function useMuted() {
  return useSyncExternalStore(
    (listener) => {
      listeners.add(listener)
      return () => listeners.delete(listener)
    },
    readMuted,
    () => false,
  )
}

function audio() {
  if (context) return context
  const Context =
    globalThis.AudioContext ?? (globalThis as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext
  if (!Context) return null
  context = new Context()
  master = context.createGain()
  master.gain.value = readMuted() ? 0 : 1
  master.connect(context.destination)
  return context
}

/** Lets the browser play sound from now on; call it from a tap. */
export function unlockSound() {
  const ctx = audio()
  if (ctx?.state === 'suspended') void ctx.resume()
}

function noiseBuffer(ctx: AudioContext) {
  if (!noise) {
    noise = ctx.createBuffer(1, ctx.sampleRate * 2, ctx.sampleRate)
    const data = noise.getChannelData(0)
    for (let i = 0; i < data.length; i++) data[i] = Math.random() * 2 - 1
  }
  return noise
}

/** A note with a quick attack and a soft fade, optionally sliding to another pitch. */
function tone(
  ctx: AudioContext,
  frequency: number,
  start: number,
  duration: number,
  { type = 'sine', volume = 0.2, to }: { type?: OscillatorType; volume?: number; to?: number } = {},
) {
  const oscillator = ctx.createOscillator()
  const gain = ctx.createGain()
  oscillator.type = type
  oscillator.frequency.setValueAtTime(frequency, start)
  if (to) oscillator.frequency.exponentialRampToValueAtTime(to, start + duration)
  gain.gain.setValueAtTime(0.0001, start)
  gain.gain.exponentialRampToValueAtTime(volume, start + 0.01)
  gain.gain.exponentialRampToValueAtTime(0.0001, start + duration)
  oscillator.connect(gain).connect(master!)
  oscillator.start(start)
  oscillator.stop(start + duration + 0.05)
}

/** A burst of filtered noise: water, steps, wind. */
function hiss(
  ctx: AudioContext,
  start: number,
  duration: number,
  { frequency, to, q = 1, volume = 0.2 }: { frequency: number; to?: number; q?: number; volume?: number },
) {
  const source = ctx.createBufferSource()
  const filter = ctx.createBiquadFilter()
  const gain = ctx.createGain()
  source.buffer = noiseBuffer(ctx)
  filter.type = 'bandpass'
  filter.Q.value = q
  filter.frequency.setValueAtTime(frequency, start)
  if (to) filter.frequency.exponentialRampToValueAtTime(to, start + duration)
  gain.gain.setValueAtTime(0.0001, start)
  gain.gain.exponentialRampToValueAtTime(volume, start + duration * 0.2)
  gain.gain.exponentialRampToValueAtTime(0.0001, start + duration)
  source.connect(filter).connect(gain).connect(master!)
  source.start(start, Math.random())
  source.stop(start + duration + 0.05)
}

const C5 = 523.25
const E5 = 659.25
const G5 = 783.99
const C6 = 1046.5
const E6 = 1318.5
const G6 = 1568

export function playSound(effect: SoundEffect) {
  const ctx = audio()
  if (!ctx || readMuted()) return
  const now = ctx.currentTime
  switch (effect) {
    case 'tap':
      tone(ctx, 660, now, 0.08, { type: 'triangle', volume: 0.12 })
      break
    case 'step':
      hiss(ctx, now, 0.08, { frequency: 260, q: 2, volume: 0.25 })
      tone(ctx, 120, now, 0.07, { volume: 0.08, to: 70 })
      break
    case 'splash':
      hiss(ctx, now, 0.4, { frequency: 1400, to: 500, q: 0.7, volume: 0.22 })
      hiss(ctx, now + 0.12, 0.25, { frequency: 2200, to: 900, q: 1.2, volume: 0.08 })
      break
    case 'correct':
      ;[C6, E6, G6].forEach((note, index) => tone(ctx, note, now + index * 0.09, 0.18, { type: 'triangle' }))
      break
    case 'wrong':
      tone(ctx, 310, now, 0.18, { type: 'triangle', volume: 0.15, to: 240 })
      tone(ctx, 240, now + 0.16, 0.28, { type: 'triangle', volume: 0.15, to: 160 })
      break
    case 'hit':
      hiss(ctx, now, 0.18, { frequency: 500, q: 0.8, volume: 0.3 })
      tone(ctx, 220, now, 0.22, { type: 'square', volume: 0.06, to: 60 })
      break
    case 'star':
      tone(ctx, G6, now, 0.25, { volume: 0.18, to: C6 * 2 })
      tone(ctx, E6 * 2, now + 0.05, 0.2, { volume: 0.06 })
      break
    case 'win':
      ;[C5, E5, G5, C6].forEach((note, index) => tone(ctx, note, now + index * 0.12, 0.3, { type: 'triangle' }))
      ;[C5, E5, G5, C6].forEach((note) => tone(ctx, note, now + 0.5, 0.9, { type: 'triangle', volume: 0.1 }))
      break
    case 'rustle':
      // Leaves shaking: a few quick, bright brushes.
      ;[0, 0.09, 0.2, 0.27].forEach((at) =>
        hiss(ctx, now + at, 0.12, { frequency: 3200, to: 1800, q: 0.6, volume: 0.12 }),
      )
      break
    case 'reveal':
      hiss(ctx, now, 0.35, { frequency: 900, to: 3000, q: 0.8, volume: 0.1 })
      ;[E6, G6, C6 * 2, E6 * 2].forEach((note, index) =>
        tone(ctx, note, now + 0.1 + index * 0.07, 0.3, { volume: 0.09 }),
      )
      break
    case 'whoosh':
      hiss(ctx, now, 0.5, { frequency: 400, to: 2400, q: 0.9, volume: 0.15 })
      break
  }
}

/**
 * The forest of the discovery walk (owner 2026-10-24: mysterious, not dull): wind in the leaves, crickets, and a bird
 * now and then. Returns the function that stops it. Like the sea, nothing piles up while the sound is off.
 */
export function startForest() {
  const ctx = audio()
  if (!ctx) return () => {}

  const wind = ctx.createBufferSource()
  const filter = ctx.createBiquadFilter()
  const gain = ctx.createGain()
  const gust = ctx.createOscillator()
  const gustDepth = ctx.createGain()
  wind.buffer = noiseBuffer(ctx)
  wind.loop = true
  filter.type = 'lowpass'
  filter.frequency.value = 380
  gain.gain.value = 0.05
  gust.frequency.value = 0.08
  gustDepth.gain.value = 0.035
  gust.connect(gustDepth).connect(gain.gain)
  wind.connect(filter).connect(gain).connect(master!)
  wind.start()
  gust.start()

  const timer = setInterval(() => {
    if (ctx.state !== 'running' || readMuted()) return
    const now = ctx.currentTime
    // Crickets: short high trills.
    for (let i = 0; i < 3; i++) tone(ctx, 4400 + Math.random() * 300, now + i * 0.07, 0.04, { volume: 0.012 })
    // A bird, sometimes: two or three quick rising chirps.
    if (Math.random() < 0.35) {
      const start = now + Math.random() * 0.6
      const pitch = 1800 + Math.random() * 1200
      const chirps = 2 + Math.floor(Math.random() * 2)
      for (let i = 0; i < chirps; i++) {
        tone(ctx, pitch, start + i * 0.13, 0.09, { volume: 0.05, to: pitch * 1.5 })
      }
    }
    // An owl, rarely.
    if (Math.random() < 0.06) {
      tone(ctx, 420, now + 0.2, 0.35, { volume: 0.05, to: 380 })
      tone(ctx, 400, now + 0.75, 0.5, { volume: 0.05, to: 340 })
    }
  }, 1300)

  return () => {
    clearInterval(timer)
    wind.stop()
    gust.stop()
    gain.disconnect()
  }
}

// A gentle tune on a pentatonic scale, one note per eighth: 0 is a rest.
const tune = [G5, E5, C5, E5, G5, 0, C6, 0, 880, G5, E5, G5, C5, 0, 0, 0]
const eighth = 0.28

/**
 * The background of the map: waves rolling in and out, and the music-box tune. Returns the function that stops it.
 * Notes are scheduled one bar ahead while the sound is running, so a suspended or muted page piles nothing up.
 */
export function startAmbience() {
  const ctx = audio()
  if (!ctx) return () => {}

  const waves = ctx.createBufferSource()
  const filter = ctx.createBiquadFilter()
  const gain = ctx.createGain()
  const swell = ctx.createOscillator()
  const swellDepth = ctx.createGain()
  waves.buffer = noiseBuffer(ctx)
  waves.loop = true
  filter.type = 'lowpass'
  filter.frequency.value = 520
  gain.gain.value = 0.06
  swell.frequency.value = 0.12
  swellDepth.gain.value = 0.045
  swell.connect(swellDepth).connect(gain.gain)
  waves.connect(filter).connect(gain).connect(master!)
  waves.start()
  swell.start()

  let next = 0
  const schedule = () => {
    if (ctx.state !== 'running' || readMuted()) return
    next = Math.max(next, ctx.currentTime + 0.05)
    while (next < ctx.currentTime + tune.length * eighth) {
      const step = Math.round(next / eighth) % tune.length
      const note = tune[step]!
      if (note) tone(ctx, note, next, eighth * 1.8, { type: 'triangle', volume: 0.035 })
      next += eighth
    }
  }
  schedule()
  const timer = setInterval(schedule, 1000)

  return () => {
    clearInterval(timer)
    waves.stop()
    swell.stop()
    gain.disconnect()
  }
}

/**
 * Plays a sound file the Admin linked (a word's sound, S3.9), unless sound is off. Resolves when it ends, fails or
 * has played for `limit` seconds, so a long or broken file never holds the child up.
 */
export function playClip(url: string, limit = 4) {
  if (readMuted()) return Promise.resolve()
  return new Promise<void>((resolve) => {
    const clip = new Audio(url)
    const done = () => {
      clearTimeout(timer)
      resolve()
    }
    const timer = setTimeout(() => {
      clip.pause()
      done()
    }, limit * 1000)
    clip.addEventListener('ended', done)
    clip.addEventListener('error', done)
    void Promise.resolve(clip.play()).catch(done)
  })
}

/**
 * Says an English word or sentence with the device's voice, slowly, for a child. When the app says it by itself
 * (`auto`), it stays quiet with sound off; a tap on "Listen" always speaks.
 */
export function speak(text: string, { auto = false }: { auto?: boolean } = {}) {
  if (!('speechSynthesis' in globalThis) || (auto && readMuted())) return
  const utterance = new SpeechSynthesisUtterance(text)
  utterance.lang = 'en-US'
  utterance.rate = 0.8
  speechSynthesis.cancel()
  speechSynthesis.speak(utterance)
}
