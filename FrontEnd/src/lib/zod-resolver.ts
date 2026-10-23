import type { FieldErrors, FieldValues, Resolver } from 'react-hook-form'
import type { z } from 'zod'

/**
 * react-hook-form resolver for a zod schema. Written here instead of using `@hookform/resolvers`, whose many optional
 * peer dependencies conflict with each other on install. Each field gets the first message zod reports for it.
 */
export function zodResolver<Schema extends z.ZodType<FieldValues, FieldValues>>(
  schema: Schema,
): Resolver<z.input<Schema>, unknown, z.output<Schema>> {
  return async (values) => {
    const result = await schema.safeParseAsync(values)
    if (result.success) {
      return { values: result.data, errors: {} }
    }

    const errors: Record<string, unknown> = {}
    for (const issue of result.error.issues) {
      setFirst(errors, issue.path.map(String), { type: issue.code, message: issue.message })
    }
    return { values: {}, errors: errors as FieldErrors<z.input<Schema>> }
  }
}

// Writes the error at its (possibly nested) path unless an earlier issue already did.
function setFirst(target: Record<string, unknown>, path: string[], error: { type: string; message: string }) {
  const [head = 'root', ...rest] = path
  if (rest.length === 0) {
    target[head] ??= error
    return
  }
  const child = (target[head] ??= {}) as Record<string, unknown>
  setFirst(child, rest, error)
}
