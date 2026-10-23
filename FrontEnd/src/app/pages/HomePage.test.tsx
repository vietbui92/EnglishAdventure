import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { HomePage } from '@/app/pages/HomePage'
import { mockApi, problem } from '@/test/api-mocks'
import { renderWithProviders } from '@/test/render'
import { server } from '@/test/server'

const statusOk = () => ({ message: 'ready', isFoundationExample: true, echo: null })

describe('HomePage', () => {
  it('links to the three areas in Vietnamese and reports the API as ready', async () => {
    server.use(mockApi('get', '/api/v1/foundation/status', statusOk))

    renderWithProviders(<HomePage />)

    expect(screen.getByRole('link', { name: /Quản trị/ }).getAttribute('href')).toBe('/admin')
    expect(screen.getByRole('link', { name: /Phụ huynh/ }).getAttribute('href')).toBe('/parent')
    expect(screen.getByRole('link', { name: /Học và chơi/ }).getAttribute('href')).toBe('/play')
    expect(await screen.findByText('API đã sẵn sàng')).toBeTruthy()
  })

  it('reports an unreachable API with the error kind', async () => {
    server.use(mockApi('get', '/api/v1/foundation/status', () => problem(502)))

    renderWithProviders(<HomePage />)

    expect(await screen.findByText('Không kết nối được API (server)')).toBeTruthy()
  })

  it('switches the interface to English', async () => {
    server.use(mockApi('get', '/api/v1/foundation/status', statusOk))
    const { user } = renderWithProviders(<HomePage />)

    await user.click(screen.getByRole('button', { name: 'English' }))

    expect(screen.getByRole('heading', { name: 'Choose an area' })).toBeTruthy()
    expect(document.documentElement.lang).toBe('en')
  })
})
