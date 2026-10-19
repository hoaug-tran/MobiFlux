import assert from 'node:assert/strict'
import { api, ApiFailure } from './src/api.ts'
const originalFetch=globalThis.fetch
try {
 globalThis.fetch=async()=>{throw new TypeError('Failed to fetch')}
 await assert.rejects(api('devices'),error=>error instanceof ApiFailure&&error.code==='NETWORK_UNAVAILABLE'&&!error.message.includes('fetch'))
 globalThis.fetch=async()=>new Response('<html>Bad gateway</html>',{status:502})
 await assert.rejects(api('devices'),error=>error instanceof ApiFailure&&error.message.includes('502'))
 globalThis.fetch=async()=>new Response(JSON.stringify({succeeded:false,error:{code:'ValidationFailed',message:'Tên chưa hợp lệ.'},correlationId:'check-1'}),{status:400})
 await assert.rejects(api('devices'),error=>error instanceof ApiFailure&&error.correlationId==='check-1'&&error.message==='Tên chưa hợp lệ.')
 const originalTimeout=AbortSignal.timeout
 try {
  AbortSignal.timeout=()=>AbortSignal.abort(new DOMException('Timeout','TimeoutError'))
  globalThis.fetch=async(_url,options)=>{throw options.signal.reason}
  await assert.rejects(api('devices'),error=>error instanceof ApiFailure&&error.code==='REQUEST_TIMEOUT')
 } finally {AbortSignal.timeout=originalTimeout}
 const controller=new AbortController()
 controller.abort(new DOMException('Cancelled','AbortError'))
 globalThis.fetch=async(_url,options)=>{throw options.signal.reason}
 await assert.rejects(api('devices','GET',undefined,controller.signal),error=>error.name==='AbortError')
 globalThis.fetch=async()=>({status:200,ok:true,json:async()=>{throw new TypeError('terminated')}})
 await assert.rejects(api('devices'),error=>error instanceof ApiFailure&&error.code==='RESPONSE_INTERRUPTED')
 console.log('PASS: network failure, non-JSON gateway error and API correlation')
} finally {globalThis.fetch=originalFetch}


