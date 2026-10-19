export interface Device {id:string;name:string;adbSerial:string;enabled:boolean;actualState:string;lastSeenAt:string|null;carrier:string|null;battery:string|null;wanIp:string|null}
export interface Endpoint {id:string;name:string;bindAddress:string;port:number;protocol:string;deviceId:string|null;poolId:string|null;maxConnections:number;enabled:boolean;running:boolean;username:string|null;allowedClients:string}
export interface Pool {id:string;name:string;strategy:string;stickyTtlSeconds:number;enabled:boolean;members:{deviceId:string;weight:number;priority:number;enabled:boolean}[]}
export interface Session {id:string;endpointId:string;deviceId:string;destinationHost:string;destinationPort:number;startedAt:string;endedAt:string;bytesUp:number;bytesDown:number;closeReason:string}
export interface Rotation {currentIp:string;previousIp:string;changedAt:string;status:string;duration:string;deviceId:string|null}
export interface Runtime {at:string;activeConnections:number;bytesUp:number;bytesDown:number;openedSessions:number;completedSessions:number;failedSessions:number;readyDevices:number;runningProxies:number;traffic:{uploadMbps:number;downloadMbps:number}}
export interface TrafficSummary {sessionCount:number;bytesUp:number;bytesDown:number;mostRecentSessionAt:string|null;failedSessions:number;rotationsCount:number}
export function formatBytes(bytes:number|undefined):string{
  if(!bytes||bytes<=0)return '0 B'
  const k=1024,sizes=['B','KiB','MiB','GiB','TiB'],i=Math.floor(Math.log(bytes)/Math.log(k))
  return `${parseFloat((bytes/Math.pow(k,i)).toFixed(2))} ${sizes[i]}`
}
export class ApiFailure extends Error {code:string;correlationId:string;fields?:Record<string,string[]>;constructor(message:string,code:string,correlationId:string,fields?:Record<string,string[]>){super(message);this.code=code;this.correlationId=correlationId;this.fields=fields}}
export async function api<T>(path:string,method='GET',body?:unknown,signal?:AbortSignal):Promise<T>{
 const timeout=AbortSignal.timeout(90000)
 const requestSignal=signal?AbortSignal.any([signal,timeout]):timeout
 let response:Response
 try { response=await fetch('/api/v1/mobiflux/'+path,{method,headers:body===undefined?undefined:{'Content-Type':'application/json'},body:body===undefined?undefined:JSON.stringify(body),signal:requestSignal,credentials:'same-origin'}) } catch(error) { if(signal?.aborted)throw error;if(timeout.aborted)throw new ApiFailure('Dịch vụ phản hồi quá lâu. Kiểm tra kết nối rồi thử lại.','REQUEST_TIMEOUT','');throw new ApiFailure('Không kết nối được dịch vụ. Kiểm tra MobiFlux Service đang chạy rồi thử lại.','NETWORK_UNAVAILABLE','') }
 if(response.status===204)return undefined as T
 let envelope
 try { envelope=await response.json() } catch(error) {
  if(signal?.aborted)throw error
  if(timeout.aborted)throw new ApiFailure('Dịch vụ phản hồi quá lâu. Kiểm tra kết nối rồi thử lại.','REQUEST_TIMEOUT','')
  if(!(error instanceof SyntaxError))throw new ApiFailure('Kết nối bị gián đoạn khi nhận dữ liệu. Làm mới và thử lại.','RESPONSE_INTERRUPTED','')
 }
 if(!response.ok||!envelope?.succeeded)throw new ApiFailure(envelope?.error?.message??`Không nhận được phản hồi hợp lệ (${response.status}).`,envelope?.error?.code??'SERVICE_UNAVAILABLE',envelope?.correlationId??'',envelope?.error?.validationErrors)
 return envelope.data as T
}
