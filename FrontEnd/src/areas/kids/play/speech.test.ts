import { describe, expect, it } from 'vitest'
import { saidWord } from '@/areas/kids/play/speech'

describe('saidWord', () => {
  it('accepts the word in any guess, in any case, alone or in a short phrase', () => {
    expect(saidWord(['Cat'], 'cat')).toBe(true)
    expect(saidWord(['hat', 'a cat.'], 'cat')).toBe(true)
    expect(saidWord(['Ice cream!'], 'ice cream')).toBe(true)
  })

  it('lets a longer word be one letter off, but not a short one', () => {
    expect(saidWord(['elefant'], 'elephant')).toBe(false)
    expect(saidWord(['rabit'], 'rabbit')).toBe(true)
    expect(saidWord(['dug'], 'dog')).toBe(false)
  })

  it('does not take a word that only contains the target', () => {
    expect(saidWord(['category'], 'cat')).toBe(false)
    expect(saidWord([], 'cat')).toBe(false)
  })
})
