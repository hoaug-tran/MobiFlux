import { useCallback, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { Alert, AlertTitle, Snackbar } from '@mui/material'
import { Context } from './toast'
import type { Notice } from './toast'
export function ToastProvider({ children }: { children: ReactNode }) {
  const [queue, setQueue] = useState<(Notice & { id: number })[]>([])
  const sequence = useRef(0)
  const notify = useCallback((notice: Notice) => {
    setQueue(current => current.some(item => item.message === notice.message && item.severity === notice.severity) ? current : [...current.slice(0, 3), { ...notice, id: ++sequence.current }])
  }, [])
  const close = () => setQueue(current => current.slice(1))
  const active = queue[0]
  return <Context.Provider value={notify}>{children}<Snackbar key={active?.id} open={!!active} anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }} autoHideDuration={active?.severity === 'error' ? null : 5000} onClose={(_, reason) => { if (reason !== 'clickaway') close() }}><Alert severity={active?.severity} variant="filled" onClose={close} sx={{ width: '100%', maxWidth: 440, boxShadow: '0 12px 40px #0003' }}><AlertTitle>{active?.severity === 'error' ? 'Chưa hoàn tất thao tác' : active?.severity === 'success' ? 'Đã hoàn tất' : 'Thông báo'}</AlertTitle>{active?.message}{active?.details && <details><summary>Chi tiết để tra cứu</summary>{active.details}</details>}</Alert></Snackbar></Context.Provider>
}



