import { useId, useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { Alert, Box, Button, Paper, Stack, ToggleButton, ToggleButtonGroup, Typography, Chip } from '@mui/material'
import {
  ArrowDownward,
  ArrowUpward,
  Cable,
  Dns,
  Groups,
  OpenInNew,
  Pause,
  PlayArrow,
  DataUsage,
  SwapVert,
  CheckCircleOutlined,
  ErrorOutlined,
  Autorenew
} from '@mui/icons-material'
import { api, formatBytes } from './api'
import type { Device, Endpoint, Pool, Runtime, TrafficSummary } from './api'
import type { useRuntime } from './realtime'

type Series = { label: string; color: string; read: (sample: Runtime) => number }
const traffic: Series[] = [
  { label: 'Tải xuống', color: '#10b981', read: s => s.traffic.downloadMbps },
  { label: 'Tải lên', color: '#2563eb', read: s => s.traffic.uploadMbps }
]
const connections: Series[] = [{ label: 'Kết nối', color: '#0284c7', read: s => s.activeConnections }]
const failures: Series[] = [{ label: 'Phiên lỗi tích lũy', color: '#f59e0b', read: s => s.failedSessions }]
const bytes = (value: number | undefined) => (value === undefined ? 'Chưa có mẫu' : `${(value / 1048576).toLocaleString('vi-VN', { maximumFractionDigits: 2 })} MiB`)

function Chart({ samples, series, unit, compact = false }: { samples: Runtime[]; series: Series[]; unit: string; compact?: boolean }) {
  const id = useId().replaceAll(':', '')
  const [hover, setHover] = useState<number>()
  const [hidden, setHidden] = useState<string[]>([])
  const visible = series.filter(s => !hidden.includes(s.label))
  const max = Math.max(1, ...samples.flatMap(s => visible.map(line => line.read(s)))) * 1.12
  const height = compact ? 160 : 270
  const left = 48, right = 780, top = 20, bottom = height - 30
  const first = Date.parse(samples[0]?.at ?? '')
  const last = Date.parse(samples.at(-1)?.at ?? '')
  const x = (sample: Runtime) => left + (last === first ? 0 : (Date.parse(sample.at) - first) / (last - first)) * (right - left)
  const y = (value: number) => bottom - (value / max) * (bottom - top)
  const selected = hover === undefined ? samples.at(-1) : samples[Math.min(hover, samples.length - 1)]

  return (
    <Box>
      <Stack direction="row" spacing={1} sx={{ mb: 2, flexWrap: 'wrap' }}>
        {series.map(line => (
          <Button
            key={line.label}
            size="small"
            aria-pressed={!hidden.includes(line.label)}
            onClick={() => setHidden(current => (current.includes(line.label) ? current.filter(v => v !== line.label) : [...current, line.label]))}
            sx={{ color: line.color, opacity: hidden.includes(line.label) ? 0.4 : 1 }}
          >
            <Box sx={{ width: 7, height: 7, borderRadius: '50%', bgcolor: line.color, mr: 1 }} />
            {line.label} · {selected ? line.read(selected).toFixed(unit === 'Mbps' ? 2 : 0) : '—'} {unit}
          </Button>
        ))}
      </Stack>
      {samples.length < 2 ? (
        <Box sx={{ height, display: 'grid', placeItems: 'center', color: 'text.secondary', border: '1px dashed', borderColor: 'divider', borderRadius: 2 }}>
          Đang chờ đủ hai mẫu từ service để vẽ biểu đồ.
        </Box>
      ) : (
        <svg
          viewBox={`0 0 800 ${height}`}
          tabIndex={0}
          onKeyDown={event => {
            if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') {
              event.preventDefault()
              setHover(current => Math.max(0, Math.min(samples.length - 1, (current ?? samples.length - 1) + (event.key === 'ArrowLeft' ? -1 : 1))))
            }
          }}
          role="img"
          aria-label={`Biểu đồ ${series.map(s => s.label).join(', ')} theo thời gian`}
          style={{ width: '100%', display: 'block', overflow: 'visible' }}
          onMouseLeave={() => setHover(undefined)}
          onMouseMove={event => {
            const bounds = event.currentTarget.getBoundingClientRect()
            const position = ((event.clientX - bounds.left) / bounds.width * 800 - left) / (right - left)
            setHover(Math.min(samples.length - 1, Math.max(0, Math.round(position * (samples.length - 1)))))
          }}
        >
          <defs>
            {series.map((line, index) => (
              <linearGradient key={line.label} id={`${id}-${index}`} x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor={line.color} stopOpacity="0.25" />
                <stop offset="100%" stopColor={line.color} stopOpacity="0.01" />
              </linearGradient>
            ))}
          </defs>
          {[0, 0.25, 0.5, 0.75, 1].map(t => (
            <g key={t}>
              <line x1={left} x2={right} y1={y(max * t)} y2={y(max * t)} stroke="currentColor" opacity="0.09" strokeDasharray="4 5" />
              <text x={left - 10} y={y(max * t) + 4} textAnchor="end" fill="currentColor" opacity="0.5" fontSize="11" fontFamily="inherit">
                {(max * t).toFixed(unit === 'Mbps' ? 1 : 0)}
              </text>
            </g>
          ))}
          {visible.map((line, index) => {
            const points = samples.map(s => `${x(s)},${y(line.read(s))}`).join(' ')
            const area = `${points} ${x(samples.at(-1)!)} ${bottom} ${left} ${bottom}`
            return (
              <g key={line.label}>
                <polygon points={area} fill={`url(#${id}-${index})`} />
                <polyline points={points} fill="none" stroke={line.color} strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round" />
              </g>
            )
          })}
          {selected && (
            <g>
              <line x1={x(selected)} x2={x(selected)} y1={top} y2={bottom} stroke="currentColor" opacity="0.3" strokeDasharray="3 3" />
              <circle cx={x(selected)} cy={y(visible[0] ? visible[0].read(selected) : 0)} r="4" fill={visible[0]?.color ?? 'currentColor'} />
            </g>
          )}
        </svg>
      )}
    </Box>
  )
}

export function MarketDashboard({ runtime }: { runtime: ReturnType<typeof useRuntime> }) {
  const devices = useQuery({ queryKey: ['devices'], queryFn: ({ signal }) => api<Device[]>('devices', 'GET', undefined, signal) })
  const proxies = useQuery({ queryKey: ['proxies'], queryFn: ({ signal }) => api<Endpoint[]>('proxies', 'GET', undefined, signal) })
  const pools = useQuery({ queryKey: ['proxy-pools'], queryFn: ({ signal }) => api<Pool[]>('proxy-pools', 'GET', undefined, signal) })

  const [periodDays, setPeriodDays] = useState<number>(0)
  const trafficSummary = useQuery({
    queryKey: ['traffic-summary', periodDays],
    queryFn: ({ signal }) => api<TrafficSummary>(periodDays === -1 ? 'proxy-traffic' : `proxy-traffic?days=${periodDays}`, 'GET', undefined, signal),
    refetchInterval: 10000
  })

  const [minutes, setMinutes] = useState(5)
  const [frozen, setFrozen] = useState<Runtime[]>()
  const history = frozen ?? runtime.history
  const samples = useMemo(() => history.filter(s => Date.parse(s.at) >= Date.parse(history.at(-1)?.at ?? '') - minutes * 60000), [history, minutes])
  const sample = samples.at(-1)

  const cards = [
    { label: 'Thiết bị sẵn sàng', value: devices.data ? `${devices.data.filter(d => d.enabled && d.actualState === 'ProxyReady').length} / ${devices.data.length}` : 'Đang tải', icon: Cable, color: '#10b981', to: '/devices' },
    { label: 'Proxy đang nghe', value: proxies.data ? `${proxies.data.filter(p => p.running).length} / ${proxies.data.length}` : 'Đang tải', icon: Dns, color: '#2563eb', to: '/proxies' },
    { label: 'Pool định tuyến', value: pools.data?.length ?? 'Đang tải', icon: Groups, color: '#8b5cf6', to: '/pools' },
    { label: 'Kết nối đang mở', value: sample?.activeConnections ?? 'Chưa có mẫu', icon: OpenInNew, color: '#0284c7', to: '/sessions' }
  ]

  const totalBytes = (trafficSummary.data?.bytesUp ?? 0) + (trafficSummary.data?.bytesDown ?? 0)
  const sessionCount = trafficSummary.data?.sessionCount ?? 0
  const failedSessions = trafficSummary.data?.failedSessions ?? 0
  const successSessions = Math.max(0, sessionCount - failedSessions)
  const successRate = sessionCount > 0 ? ((successSessions / sessionCount) * 100).toFixed(1) : '100'
  const rotationsCount = trafficSummary.data?.rotationsCount ?? 0

  return (
    <Stack spacing={2.5}>
      <Stack direction={{ xs: 'column', sm: 'row' }} sx={{ justifyContent: 'space-between', gap: 2 }}>
        <Box>
          <Typography variant="overline" color="primary" sx={{ letterSpacing: 2, fontWeight: 700 }}>TRUNG TÂM KẾT NỐI</Typography>
          <Typography component="h1" variant="h4" sx={{ fontWeight: 800, letterSpacing: '-0.02em' }}>Toàn cảnh mạng di động</Typography>
          <Typography color="text.secondary" sx={{ mt: 0.5 }}>Theo dõi băng thông, thiết bị và phiên kết nối theo thời gian thực.</Typography>
        </Box>
        <Button component={Link} to="/proxies" variant="contained" startIcon={<Dns />} sx={{ alignSelf: 'center' }}>Quản lý proxy</Button>
      </Stack>

      {(devices.error || proxies.error || pools.error) && (
        <Alert severity="error" action={<Button onClick={() => { void devices.refetch(); void proxies.refetch(); void pools.refetch() }}>Thử lại</Button>}>
          Không tải được một phần dữ liệu quản trị. Các chỉ số liên quan chưa được xác minh.
        </Alert>
      )}

      {/* Realtime Core Cards */}
      <Box className="metric-grid">
        {cards.map(card => (
          <Paper component={Link} to={card.to} key={card.label} variant="outlined" className="metric-card" sx={{ p: 2.5, textDecoration: 'none', color: 'text.primary', '--accent': card.color }}>
            <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
              <Typography variant="body2" color="text.secondary">{card.label}</Typography>
              <card.icon sx={{ color: card.color, fontSize: 21 }} />
            </Stack>
            <Typography variant="h4" sx={{ mt: 2, mb: 1, fontVariantNumeric: 'tabular-nums', fontWeight: 700 }}>{card.value}</Typography>
            <Typography variant="caption" sx={{ color: card.color }}>Xem chi tiết và thao tác</Typography>
          </Paper>
        ))}
      </Box>

      {/* Date Range Analytics Filter & Metrics */}
      <Paper variant="outlined" sx={{ p: { xs: 2, md: 2.5 }, borderRadius: 2 }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' }, gap: 1.5, mb: 2 }}>
          <Box>
            <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>Thống kê theo chu kỳ</Typography>
            <Typography variant="caption" color="text.secondary">Lọc dữ liệu lưu lượng, phiên proxy và đổi IP cellular</Typography>
          </Box>
          <ToggleButtonGroup
            exclusive
            size="small"
            value={periodDays}
            onChange={(_, val: number | null) => { if (val !== null) setPeriodDays(val) }}
            sx={{ flexWrap: 'wrap' }}
          >
            <ToggleButton value={0} sx={{ px: 1.5, py: 0.4, fontSize: '0.8125rem' }}>Hôm nay</ToggleButton>
            <ToggleButton value={3} sx={{ px: 1.5, py: 0.4, fontSize: '0.8125rem' }}>3 ngày</ToggleButton>
            <ToggleButton value={7} sx={{ px: 1.5, py: 0.4, fontSize: '0.8125rem' }}>7 ngày</ToggleButton>
            <ToggleButton value={30} sx={{ px: 1.5, py: 0.4, fontSize: '0.8125rem' }}>30 ngày</ToggleButton>
            <ToggleButton value={60} sx={{ px: 1.5, py: 0.4, fontSize: '0.8125rem' }}>60 ngày</ToggleButton>
            <ToggleButton value={-1} sx={{ px: 1.5, py: 0.4, fontSize: '0.8125rem' }}>Toàn bộ</ToggleButton>
          </ToggleButtonGroup>
        </Stack>

        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', md: 'repeat(5, 1fr)' }, gap: 1.5 }}>
          <Paper variant="outlined" sx={{ p: 2, bgcolor: 'background.default' }}>
            <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
              <Typography variant="caption" color="text.secondary">Tổng lưu lượng</Typography>
              <DataUsage sx={{ color: '#0ea5e9', fontSize: 18 }} />
            </Stack>
            <Typography variant="h5" sx={{ mt: 1, mb: 0.5, fontWeight: 700, color: '#0ea5e9' }}>{formatBytes(totalBytes)}</Typography>
            <Typography variant="caption" color="text.secondary">
              ▲ {formatBytes(trafficSummary.data?.bytesUp ?? 0)} · ▼ {formatBytes(trafficSummary.data?.bytesDown ?? 0)}
            </Typography>
          </Paper>

          <Paper variant="outlined" sx={{ p: 2, bgcolor: 'background.default' }}>
            <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
              <Typography variant="caption" color="text.secondary">Tổng phiên kết nối</Typography>
              <SwapVert sx={{ color: '#2563eb', fontSize: 18 }} />
            </Stack>
            <Typography variant="h5" sx={{ mt: 1, mb: 0.5, fontWeight: 700 }}>{sessionCount.toLocaleString('vi-VN')}</Typography>
            <Typography variant="caption" color="text.secondary">{successSessions.toLocaleString('vi-VN')} phiên thành công</Typography>
          </Paper>

          <Paper variant="outlined" sx={{ p: 2, bgcolor: 'background.default' }}>
            <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
              <Typography variant="caption" color="text.secondary">Phiên lỗi / Thất bại</Typography>
              <ErrorOutlined sx={{ color: failedSessions > 0 ? '#ef4444' : '#10b981', fontSize: 18 }} />
            </Stack>
            <Typography variant="h5" sx={{ mt: 1, mb: 0.5, fontWeight: 700, color: failedSessions > 0 ? '#ef4444' : 'text.primary' }}>
              {failedSessions.toLocaleString('vi-VN')}
            </Typography>
            <Typography variant="caption" color="text.secondary">{failedSessions === 0 ? 'Không có phiên đứt đoạn' : 'Cần kiểm tra thiết bị'}</Typography>
          </Paper>

          <Paper variant="outlined" sx={{ p: 2, bgcolor: 'background.default' }}>
            <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
              <Typography variant="caption" color="text.secondary">Tỷ lệ thành công</Typography>
              <CheckCircleOutlined sx={{ color: '#10b981', fontSize: 18 }} />
            </Stack>
            <Typography variant="h5" sx={{ mt: 1, mb: 0.5, fontWeight: 700, color: '#10b981' }}>{successRate}%</Typography>
            <Typography variant="caption" color="text.secondary">Hiệu suất định tuyến</Typography>
          </Paper>

          <Paper variant="outlined" sx={{ p: 2, bgcolor: 'background.default' }}>
            <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
              <Typography variant="caption" color="text.secondary">Đổi IP Cellular</Typography>
              <Autorenew sx={{ color: '#8b5cf6', fontSize: 18 }} />
            </Stack>
            <Typography variant="h5" sx={{ mt: 1, mb: 0.5, fontWeight: 700, color: '#8b5cf6' }}>{rotationsCount.toLocaleString('vi-VN')}</Typography>
            <Typography variant="caption" color="text.secondary">Lần xoay IP hoàn tất</Typography>
          </Paper>
        </Box>
      </Paper>

      {/* Realtime Bandwidth Chart */}
      <Paper variant="outlined" sx={{ p: { xs: 2, md: 3 }, overflow: 'hidden' }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} sx={{ justifyContent: 'space-between', gap: 2, mb: 3 }}>
          <Box>
            <Typography variant="h6">Nhịp lưu lượng thời gian thực</Typography>
            <Typography variant="body2" color="text.secondary">Băng thông quan sát toàn service · Mbps</Typography>
          </Box>
          <Stack direction="row" spacing={1}>
            <ToggleButtonGroup exclusive size="small" value={minutes} onChange={(_, value: number | null) => { if (value) setMinutes(value) }}>
              {[1, 5, 15].map(value => <ToggleButton key={value} value={value}>{value} phút</ToggleButton>)}
            </ToggleButtonGroup>
            <Button size="small" variant="outlined" startIcon={frozen ? <PlayArrow /> : <Pause />} onClick={() => setFrozen(current => current ? undefined : [...runtime.history])}>
              {frozen ? 'Tiếp tục' : 'Tạm dừng'}
            </Button>
          </Stack>
        </Stack>
        <Chart samples={samples} series={traffic} unit="Mbps" />
      </Paper>

      <Box className="chart-grid">
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Typography variant="h6" sx={{ mb: 1 }}>Kết nối đồng thời</Typography>
          <Chart samples={samples} series={connections} unit="phiên" compact />
        </Paper>
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Typography variant="h6" sx={{ mb: 1 }}>Theo dõi tỷ lệ lỗi</Typography>
          <Chart samples={samples} series={failures} unit="lỗi" compact />
        </Paper>
      </Box>

      <Box className="chart-grid">
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
            <Typography variant="h6">Thiết bị & đường ra</Typography>
            <Button component={Link} to="/devices">Tất cả thiết bị</Button>
          </Stack>
          {devices.data?.length === 0 ? (
            <Alert severity="info">Cắm Android qua USB, cấp quyền gỡ lỗi rồi dò thiết bị.</Alert>
          ) : (
            devices.data?.map(d => (
              <Stack key={d.id} direction="row" sx={{ py: 1.5, gap: 2, alignItems: 'center', borderBottom: 1, borderColor: 'divider' }}>
                <Cable color="primary" />
                <Box sx={{ flex: 1, minWidth: 0 }}>
                  <Typography sx={{ fontWeight: 600 }}>{d.name}</Typography>
                  <Typography variant="caption" color="text.secondary">
                    {d.carrier ?? 'Chưa rõ nhà mạng'} · {d.wanIp ?? 'Chưa có IP'}
                  </Typography>
                </Box>
                <Chip size="small" color={d.actualState === 'ProxyReady' ? 'success' : 'default'} label={d.actualState === 'ProxyReady' ? 'Sẵn sàng' : d.actualState} />
              </Stack>
            ))
          )}
        </Paper>
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Typography variant="h6">Dung lượng đã chuyển</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>Trong tiến trình service hiện tại</Typography>
          <Box className="volume-grid">
            <Box><ArrowDownward sx={{ color: '#10b981' }} /><Typography variant="h5">{bytes(sample?.bytesDown)}</Typography><Typography variant="caption">Tải xuống</Typography></Box>
            <Box><ArrowUpward sx={{ color: '#2563eb' }} /><Typography variant="h5">{bytes(sample?.bytesUp)}</Typography><Typography variant="caption">Tải lên</Typography></Box>
          </Box>
          <Alert severity="info" sx={{ mt: 3 }}>Biểu đồ giữ tối đa 15 phút từ lúc mở trang. Dừng quan sát không dừng proxy. Mất kết nối có thể tạo khoảng trống dữ liệu.</Alert>
        </Paper>
      </Box>
    </Stack>
  )
}
