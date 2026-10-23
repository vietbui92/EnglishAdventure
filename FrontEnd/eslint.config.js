import js from '@eslint/js'
import reactHooks from 'eslint-plugin-react-hooks'
import { defineConfig, globalIgnores } from 'eslint/config'
import tseslint from 'typescript-eslint'

// Each area (src/areas/<area>) uses only its own files and src/shared, so a change in one area cannot break another.
// Only src/app (and tests, which render the whole app) may bring the areas together.
const areas = ['public', 'admin', 'parent', 'kids']

const relativeParent = { group: ['../*'], message: 'Import with the @/ alias.' }
const appImport = { group: ['@/app', '@/app/*'], message: 'Only src/main.tsx and tests may import src/app.' }
const otherAreas = (area) => ({
  group: ['@/areas/*', `!@/areas/${area}`, `!@/areas/${area}/**`],
  message: `The ${area} area may import only its own files and @/shared. Move shared code to src/shared.`,
})

export default defineConfig([
  globalIgnores(['dist', 'coverage', 'src/shared/api/schema.d.ts']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [js.configs.recommended, tseslint.configs.recommended, reactHooks.configs.flat.recommended],
    rules: {
      // TypeScript already reports undefined names, including browser globals.
      'no-undef': 'off',
    },
  },
  ...areas.map((area) => ({
    files: [`src/areas/${area}/**/*.{ts,tsx}`],
    ignores: ['**/*.test.{ts,tsx}'],
    rules: { 'no-restricted-imports': ['error', { patterns: [otherAreas(area), appImport, relativeParent] }] },
  })),
  ...areas.map((area) => ({
    files: [`src/areas/${area}/**/*.test.{ts,tsx}`],
    rules: { 'no-restricted-imports': ['error', { patterns: [otherAreas(area), relativeParent] }] },
  })),
  {
    files: ['src/shared/**/*.{ts,tsx}'],
    // The i18n type declaration lists every area's texts (type-only).
    ignores: ['src/shared/i18n/i18next.d.ts'],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            {
              group: ['@/areas', '@/areas/*', '@/app', '@/app/*'],
              message: 'Shared code cannot depend on an area or the app.',
            },
            relativeParent,
          ],
        },
      ],
    },
  },
])
