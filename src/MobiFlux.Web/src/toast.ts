import { createContext, useContext } from 'react'
import { ApiFailure } from './api'
export type Notice = { message: string; severity: 'success' | 'error' | 'warning' | 'info'; details?: string }
export const Context = createContext<(notice: Notice) => void>(() => {})
export const useToast = () => useContext(Context)
export function errorNotice(error: Error): Notice {
  if (error instanceof TypeError) return { severity: 'error', message: 'Không kết nối được service. Kiểm tra service đang chạy rồi thử lại.' }
  return { severity: 'error', message: error instanceof ApiFailure ? error.message : 'Thao tác chưa hoàn tất. Thử lại; nếu lỗi tiếp diễn, xem chi tiết để tra cứu.', details: error instanceof ApiFailure ? [error.message, error.code, error.correlationId, ...Object.values(error.fields ?? {}).flat()].filter(Boolean).join(' · ') : undefined }
}



