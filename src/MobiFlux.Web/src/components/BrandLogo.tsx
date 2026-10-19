export function BrandLogo({ size = 28, showText = true }: { size?: number; showText?: boolean }) {
  return (
    <div style={{ display: 'inline-flex', alignItems: 'center', gap: 10, userSelect: 'none' }}>
      <svg width={size} height={size} viewBox="0 0 64 64" fill="none" xmlns="http://www.w3.org/2000/svg" style={{ flexShrink: 0 }}>
        <defs>
          <linearGradient id="mf-comp-stream-l" x1="14" y1="50" x2="32" y2="12" gradientUnits="userSpaceOnUse">
            <stop offset="0%" stopColor="#0284c7" />
            <stop offset="60%" stopColor="#06b6d4" />
            <stop offset="100%" stopColor="#38bdf8" />
          </linearGradient>
          <linearGradient id="mf-comp-stream-r" x1="32" y1="12" x2="50" y2="50" gradientUnits="userSpaceOnUse">
            <stop offset="0%" stopColor="#38bdf8" />
            <stop offset="40%" stopColor="#3b82f6" />
            <stop offset="100%" stopColor="#818cf8" />
          </linearGradient>
          <radialGradient id="mf-comp-core-glow" cx="50%" cy="50%" r="50%">
            <stop offset="0%" stopColor="#38bdf8" stopOpacity="0.9" />
            <stop offset="100%" stopColor="#0284c7" stopOpacity="0" />
          </radialGradient>
        </defs>
        <path d="M14 50 V23 C14 12 23 12 32 28" stroke="url(#mf-comp-stream-l)" strokeWidth="6" strokeLinecap="round" strokeLinejoin="round" />
        <path d="M32 28 C41 12 50 12 50 23 V50" stroke="url(#mf-comp-stream-r)" strokeWidth="6" strokeLinecap="round" strokeLinejoin="round" />
        <circle cx="32" cy="28" r="8" fill="url(#mf-comp-core-glow)" />
        <circle cx="32" cy="28" r="4" fill="#ffffff" />
        <circle cx="14" cy="50" r="3.5" fill="#38bdf8" />
        <circle cx="50" cy="50" r="3.5" fill="#818cf8" />
      </svg>
      {showText && (
        <span style={{ fontSize: 18, fontWeight: 700, letterSpacing: '-0.02em', color: 'inherit', fontFamily: 'inherit' }}>
          Mobi<span style={{ color: '#0ea5e9' }}>Flux</span>
        </span>
      )}
    </div>
  )
}
