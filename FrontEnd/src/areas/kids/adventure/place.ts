/**
 * Where the child's character stands on the map: on the mainland (no level), on a level's island, and at which lesson
 * on it. Kept on the device, per child, only so the map opens where the child left it; progress and locks come from
 * the server (S4.2).
 */
export interface AdventurePlace {
  levelId: string | null
  lessonId: string | null
  /** Back at the island's boat, looking out to sea: after defeating its boss, the next island waits (F4.7). */
  atSea?: boolean
}

const mainland: AdventurePlace = { levelId: null, lessonId: null }
const key = (childId: string) => `kids.adventure.${childId}`

export function readPlace(childId: string | undefined): AdventurePlace {
  if (!childId) return mainland
  try {
    const stored = JSON.parse(localStorage.getItem(key(childId)) ?? 'null') as Partial<AdventurePlace> | null
    return { levelId: stored?.levelId ?? null, lessonId: stored?.lessonId ?? null, atSea: stored?.atSea === true }
  } catch {
    return mainland
  }
}

export function savePlace(childId: string | undefined, place: AdventurePlace) {
  if (!childId) return
  try {
    localStorage.setItem(key(childId), JSON.stringify(place))
  } catch {
    // Without storage the map starts on the mainland next time.
  }
}
