import { useEffect, useRef, useState } from 'react'

/*
 * Speaking practice on the review cards (PD-013): the browser's speech recognition hears the child say the word.
 * It is on whenever the browser has it (owner 2026-10-24); the browser sends the voice to its maker's servers to turn
 * it into text, and nothing is sent to ours or saved. Practice is never scored.
 */

interface RecognitionAlternative {
  transcript: string
}

interface RecognitionEvent {
  results: ArrayLike<ArrayLike<RecognitionAlternative>>
}

interface Recognition {
  lang: string
  interimResults: boolean
  maxAlternatives: number
  continuous: boolean
  onresult: ((event: RecognitionEvent) => void) | null
  onerror: ((event: { error: string }) => void) | null
  onend: (() => void) | null
  start(): void
  abort(): void
}

type RecognitionConstructor = new () => Recognition

function recognitionConstructor(): RecognitionConstructor | undefined {
  const scope = globalThis as {
    SpeechRecognition?: RecognitionConstructor
    webkitSpeechRecognition?: RecognitionConstructor
  }
  return scope.SpeechRecognition ?? scope.webkitSpeechRecognition
}

/** Whether this browser can hear the child; without it the review cards only offer "Listen". */
export function canHearSpeech() {
  return recognitionConstructor() !== undefined
}

const normalize = (text: string) =>
  text
    .toLowerCase()
    .replace(/[^a-z' ]/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()

/** How many single-letter changes turn one text into the other. */
function distance(a: string, b: string) {
  const row = Array.from({ length: b.length + 1 }, (_, index) => index)
  for (let i = 1; i <= a.length; i++) {
    let previous = row[0]!
    row[0] = i
    for (let j = 1; j <= b.length; j++) {
      const current = row[j]!
      row[j] = Math.min(row[j]! + 1, row[j - 1]! + 1, previous + (a[i - 1] === b[j - 1] ? 0 : 1))
      previous = current
    }
  }
  return row[b.length]!
}

/**
 * Whether what was heard is the word. Generous, because recognition is weaker on children's voices: any of the
 * guesses may match, the word may sit inside a short phrase ("a cat"), and a longer word may be one letter off.
 */
export function saidWord(heard: string[], word: string) {
  const target = normalize(word)
  return heard.map(normalize).some((guess) => {
    if (guess === target || ` ${guess} `.includes(` ${target} `)) return true
    return target.length >= 4 && distance(guess, target) <= 1
  })
}

export type Practice =
  | { state: 'idle' }
  | { state: 'listening' }
  | { state: 'right'; heard: string }
  | { state: 'wrong'; heard: string }
  | { state: 'silent' }
  | { state: 'blocked' }

/** Listens for the child saying `word` once per `listen()`. Stops when the card changes or closes. */
export function useSpeechPractice(word: string) {
  const [practice, setPractice] = useState<Practice>({ state: 'idle' })
  const current = useRef<Recognition | null>(null)

  useEffect(() => () => current.current?.abort(), [])

  const listen = () => {
    const Constructor = recognitionConstructor()
    if (!Constructor) return
    current.current?.abort()
    const recognition = new Constructor()
    recognition.lang = 'en-US'
    recognition.interimResults = false
    recognition.continuous = false
    recognition.maxAlternatives = 5
    let finished = false
    recognition.onresult = (event) => {
      finished = true
      const alternatives = Array.from(event.results[0] ?? [], (alternative) => alternative.transcript)
      const heard = alternatives[0]?.trim() ?? ''
      setPractice(saidWord(alternatives, word) ? { state: 'right', heard } : { state: 'wrong', heard })
    }
    recognition.onerror = (event) => {
      finished = true
      setPractice({
        state: event.error === 'not-allowed' || event.error === 'service-not-allowed' ? 'blocked' : 'silent',
      })
    }
    recognition.onend = () => {
      if (!finished) setPractice({ state: 'silent' })
    }
    current.current = recognition
    setPractice({ state: 'listening' })
    recognition.start()
  }

  return { practice, listen, reset: () => setPractice({ state: 'idle' }) }
}
