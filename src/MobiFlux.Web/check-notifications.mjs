import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
const app = readFileSync(new URL('./src/App.tsx', import.meta.url), 'utf8')
const provider = readFileSync(new URL('./src/notifications.tsx', import.meta.url), 'utf8')
assert.equal((app.match(/<ToastProvider>/g) ?? []).length, 1)
assert.equal((app.match(/<Snackbar/g) ?? []).length, 0)
assert.match(provider, /vertical: 'bottom', horizontal: 'right'/)
assert.match(app, /priority:Number\(form.get\(`priority-\$\{deviceId\}`\)/)
assert.match(app, /proxy-pools\/\$\{removePool.id\}/)
console.log('PASS: shared toast placement, pool priority and delete wiring')

assert.match(app, /id="sidebar-toggle"/)
assert.match(app, /mobiflux-sidebar/)
assert.doesNotMatch(app, /maxWidth:1600/)
assert.match(app, /shape:\{borderRadius:6\}/)
assert.match(app, /password:probePassword\|\|null/)
assert.match(app, /setProbePassword\(''\)/)
assert.match(app, /networking\/interfaces/)
assert.match(app, /networking\/netstat/)
