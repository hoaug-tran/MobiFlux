import { ToastProvider } from './notifications'
import { useToast, errorNotice } from './toast'
const MarketDashboard=lazy(()=>import('./dashboard').then(module=>({default:module.MarketDashboard})))
import { lazy, Suspense, useCallback, useMemo, useState } from 'react'
import type { FormEvent, ReactNode } from 'react'
import { QueryClient, QueryClientProvider, useQuery, useQueryClient } from '@tanstack/react-query'
import { BrowserRouter, NavLink, Route, Routes, useLocation } from 'react-router-dom'
import { Alert, AppBar, Box, Button, Chip, CircularProgress, CssBaseline, Dialog, DialogActions, DialogContent, DialogTitle, Divider, FormControlLabel, IconButton, MenuItem, Paper, Stack, Switch, Table, TableBody, TableCell, TableContainer, TableHead, TablePagination, TableRow, TextField, ThemeProvider, Toolbar, Typography, createTheme, useMediaQuery } from '@mui/material'
import { Public, ChevronLeft, ChevronRight, Add, Cable, ContentCopy, DarkMode, Dashboard, DeleteOutlined, Dns, Groups, History, LightMode, PlayArrow, Refresh, Settings, Stop, SwapHoriz } from '@mui/icons-material'
import '@fontsource/inter/latin-400.css'
import '@fontsource/inter/vietnamese-400.css'
import '@fontsource/inter/latin-500.css'
import '@fontsource/inter/vietnamese-500.css'
import '@fontsource/inter/latin-600.css'
import '@fontsource/inter/vietnamese-600.css'
import { api, ApiFailure } from './api'
import type { Device, Endpoint, Pool, Rotation, Session } from './api'
import { useRuntime } from './realtime'
import { BrandLogo } from './components/BrandLogo'

const STRATEGIES = [
  { value: 'RoundRobin', label: 'Chia đều lần lượt', desc: 'Mỗi kết nối mới sẽ chuyển sang điện thoại tiếp theo theo thứ tự xoay vòng.' },
  { value: 'LeastConnections', label: 'Ưu tiên máy đang rảnh', desc: 'Tự động chọn điện thoại đang có ít kết nối nhất để vào mạng nhanh nhất.' },
  { value: 'WeightedRoundRobin', label: 'Chia theo độ ưu tiên / sức mạnh', desc: 'Điện thoại nào cài trọng số lớn hơn sẽ nhận được nhiều kết nối hơn.' },
  { value: 'Random', label: 'Chọn ngẫu nhiên', desc: 'Mỗi kết nối sẽ bốc ngẫu nhiên một điện thoại đang online.' }
]

const client=new QueryClient({defaultOptions:{queries:{retry:1,refetchInterval:15000,refetchOnWindowFocus:true}}})
const navigation=[['/','Tổng quan',Dashboard],['/devices','Thiết bị',Cable],['/proxies','Proxy',Dns],['/pools','Pool',Groups],['/rotation','Đổi IP',SwapHoriz],['/sessions','Phiên kết nối',History],['/networking','Mạng Windows',Public],['/settings','Thiết lập',Settings]] as const
const formatBytes=(value:number)=>`${(value/1048576).toLocaleString('vi-VN',{maximumFractionDigits:2})} MiB`
const date=(value:string|null)=>value?new Date(value).toLocaleString('vi-VN'):'Chưa có dữ liệu'
function useResource<T>(path:string){return useQuery({queryKey:[path],queryFn:({signal})=>api<T>(path,'GET',undefined,signal)})}
function ErrorNotice({error}:{error:Error|null}){return error?<Alert severity="error">{error.message}{error instanceof ApiFailure&&<Typography variant="caption" sx={{display:"block"}}>{error.code} · {error.correlationId}</Typography>}</Alert>:null}
function Empty({children}:{children:ReactNode}){return <Box sx={{py:7,textAlign:'center',color:'text.secondary'}}>{children}</Box>}
function Frame({title,description,action,children}:{title:string;description:string;action?:ReactNode;children:ReactNode}){return <Stack spacing={2} sx={{flex:1,display:'flex',flexDirection:'column',minHeight:'calc(100vh - 90px)'}}><Stack direction="row" sx={{justifyContent:"space-between",alignItems:"center",gap:2,flexShrink:0}}><Box><Typography variant="h4" component="h1">{title}</Typography><Typography color="text.secondary" sx={{mt:1}}>{description}</Typography></Box>{action}</Stack>{children}</Stack>}
function Shell(){
 const systemDark=useMediaQuery('(prefers-color-scheme: dark)')
 const [mode,setMode]=useState(()=>localStorage.getItem('mobiflux-theme')??'light')
 const dark=mode==='system'?systemDark:mode==='dark'
 const [collapsed,setCollapsed]=useState(()=>localStorage.getItem('mobiflux-sidebar')?localStorage.getItem('mobiflux-sidebar')==='collapsed':window.innerWidth<900)
 const rail=collapsed
 const sidebarWidth=rail?56:216
 const theme=useMemo(()=>createTheme({
   palette:{
     mode:dark?'dark':'light',
     primary:{main:dark?'#60a5fa':'#2563eb',light:dark?'#93c5fd':'#3b82f6',dark:dark?'#2563eb':'#1d4ed8'},
     background:{default:dark?'#0f172a':'#f8fafc',paper:dark?'#1e293b':'#ffffff'},
     text:{primary:dark?'#f8fafc':'#0f172a',secondary:dark?'#94a3b8':'#64748b'},
     divider:dark?'#334155':'#e2e8f0',
     action:{selected:dark?'rgba(96,165,250,0.16)':'rgba(37,99,235,0.08)'}
   },
   typography:{fontFamily:'Inter, sans-serif',h4:{fontSize:'1.8rem',fontWeight:600},h6:{fontWeight:600},button:{textTransform:'none'}},
   shape:{borderRadius:6},
   components:{
     MuiButton:{defaultProps:{disableElevation:true}},
     MuiPaper:{defaultProps:{elevation:0},styleOverrides:{root:{backgroundImage:'none',borderColor:dark?'#334155':'#e2e8f0'}}},
     MuiTableCell:{styleOverrides:{
       root:{borderColor:dark?'#334155':'#e2e8f0'},
       head:{
         fontWeight:600,
         backgroundColor:dark?'#1e293b':'#f1f5f9',
         color:dark?'#94a3b8':'#334155',
         borderBottom:dark?'1px solid #334155':'1px solid #e2e8f0',
         fontSize:'0.8125rem',
         letterSpacing:'0.02em',
         textTransform:'none',
         '&.MuiTableCell-stickyHeader':{
           backgroundColor:dark?'#1e293b':'#f1f5f9'
         }
       }
     }}
   }
 }),[dark])
 const query=useQueryClient()
 const resync=useCallback(()=>{void query.invalidateQueries()},[query])
 const runtime=useRuntime(resync)
 const location=useLocation()
 return <ThemeProvider theme={theme}><CssBaseline/><ToastProvider><Box sx={{display:'flex',minHeight:'100dvh'}}><Box component="nav" aria-label="Điều hướng chính" sx={{width:sidebarWidth,transition:'width 160ms ease',flexShrink:0,borderRight:1,borderColor:'divider',bgcolor:'background.paper',position:'fixed',height:'100dvh',pt:2,zIndex:1100}}><Stack direction="row" sx={{alignItems:"center",justifyContent:rail?'center':'space-between',px:rail?1:2,mb:3,height:36}}><Stack direction="row" spacing={1.2} sx={{alignItems:"center",justifyContent:rail?'center':'flex-start',display:'flex'}}><BrandLogo size={28} showText={!rail}/></Stack><IconButton id="sidebar-toggle" size="small" aria-label={collapsed?"Mở rộng thanh điều hướng":"Thu gọn thanh điều hướng"} aria-expanded={!rail} onClick={()=>{const next=!collapsed;setCollapsed(next);localStorage.setItem("mobiflux-sidebar",next?"collapsed":"expanded")}} sx={{color:'text.secondary','&:hover':{color:'primary.main'}}}>{rail?<ChevronRight fontSize="small"/>:<ChevronLeft fontSize="small"/>}</IconButton></Stack>{navigation.map(([url,label,Icon])=><Button id={`nav-${url==='/'?'overview':url.slice(1)}`} key={url} component={NavLink} to={url} title={label} aria-label={label} startIcon={<Icon/>} sx={{display:'flex',justifyContent:rail?'center':'flex-start',mx:0.75,mb:0.5,minWidth:0,px:1.25,py:1,width:'calc(100% - 12px)',color:location.pathname===url?'primary.main':'text.secondary',bgcolor:location.pathname===url?'action.selected':'transparent',fontWeight:location.pathname===url?600:500,'& .MuiButton-startIcon':{mr:rail?0:1.5}}}><Box sx={{display:rail?'none':'block'}}>{label}</Box></Button>)}</Box><Box sx={{ml:`${sidebarWidth}px`,width:`calc(100% - ${sidebarWidth}px)`,minWidth:0,transition:'margin-left 160ms ease,width 160ms ease',display:'flex',flexDirection:'column',minHeight:'100dvh'}}><AppBar position="sticky" color="inherit" elevation={0} sx={{borderBottom:1,borderColor:'divider'}}><Toolbar sx={{gap:1.5,minHeight:"52px!important",px:"16px!important"}}><Typography variant="body2" sx={{flex:1,fontWeight:500,color:'text.secondary'}}>Quản lý kết nối di động</Typography><Chip size="small" label={runtime.state} color={runtime.state==='Trực tiếp'?'success':'warning'} variant="outlined"/><IconButton id="theme-toggle" aria-label="Đổi giao diện sáng tối" onClick={()=>{const next=dark?'light':'dark';setMode(next);localStorage.setItem('mobiflux-theme',next)}}>{dark?<LightMode/>:<DarkMode/>}</IconButton></Toolbar></AppBar><Box component="main" sx={{p:{xs:1.5,md:2.5},width:'100%',minWidth:0,flex:1,display:'flex',flexDirection:'column'}}><Suspense fallback={<Box role="status" sx={{p:2}}><CircularProgress size={24} aria-label="Đang tải trang"/></Box>}><Routes><Route path="/" element={<Overview runtime={runtime}/>}/><Route path="/devices" element={<Devices/>}/><Route path="/proxies" element={<Proxies/>}/><Route path="/pools" element={<Pools/>}/><Route path="/rotation" element={<Rotations/>}/><Route path="/sessions" element={<Sessions/>}/><Route path="/networking" element={<Networking/>}/><Route path="/settings" element={<SettingsPage/>}/><Route path="*" element={<Frame title="Không tìm thấy trang" description="Đường dẫn không tồn tại."><Button component={NavLink} to="/">Về tổng quan</Button></Frame>}/></Routes></Suspense></Box></Box></Box></ToastProvider></ThemeProvider>
}
function Overview({runtime}:{runtime:ReturnType<typeof useRuntime>}){return <MarketDashboard runtime={runtime}/>}
function useAction(){
 const query=useQueryClient()
 const notify=useToast()
 const [busy,setBusy]=useState<string[]>([])
 const [error,setError]=useState<Error|null>(null)
 async function run(key:string,work:()=>Promise<unknown>,message?:string){
  if(busy.includes(key))return false
  setBusy(v=>[...v,key]);setError(null)
  try{await work();if(message)notify({severity:'success',message});await query.invalidateQueries();return true}
  catch(value){const failure=value instanceof Error?value:new Error(String(value));setError(failure);notify(errorNotice(failure));return false}
  finally{setBusy(v=>v.filter(x=>x!==key))}
 }
 return {busy,error,run,feedback:null}
}function Devices(){const data=useResource<Device[]>('devices');const action=useAction();return <Frame title="Thiết bị" description="Mỗi Android là một đường ra cellular độc lập." action={<Button id="discover-devices" variant="contained" startIcon={<Refresh/>} disabled={action.busy.includes('discover')} onClick={()=>void action.run('discover',()=>api('device-discovery-runs','POST'),'Đã chạy dò thiết bị.')}>Dò thiết bị</Button>}>{action.feedback}<ErrorNotice error={data.error}/>{data.isPending?<CircularProgress aria-label="Đang tải thiết bị"/>:<Paper variant="outlined" sx={{flex:1,minHeight:400,display:'flex',flexDirection:'column'}}>{!data.data?.length?<Empty>Cắm điện thoại, bật USB debugging và xác nhận quyền trên màn hình điện thoại.</Empty>:<Stack divider={<Divider/>} sx={{flex:1}}>{data.data.map(d=><Stack key={d.id} direction={{xs:'column',sm:'row'}} spacing={2} sx={{p:2.5,alignItems:{sm:"center"}}}><Box sx={{flex:1,minWidth:0}}><Typography sx={{fontWeight:600}}>{d.name}</Typography><Typography variant="body2" color="text.secondary">{d.carrier??'Chưa rõ nhà mạng'} · IP: {d.wanIp??'Chưa đo'}</Typography><Typography variant="caption">Quan sát: {date(d.lastSeenAt)}</Typography></Box><Chip label={d.actualState} color={d.actualState==='ProxyReady'?'success':'default'} variant="outlined"/><FormControlLabel label="Cho phép" control={<Switch id={`device-enabled-${d.id}`} checked={d.enabled} disabled={action.busy.includes(d.id)} onChange={(_,enabled)=>void action.run(d.id,()=>api(`devices/${d.id}`,'PATCH',{enabled}),'Đã lưu trạng thái thiết bị.')}/>}/></Stack>)}</Stack>}</Paper>}</Frame>}
function Proxies(){
 const data=useResource<Endpoint[]>('proxies'),devices=useResource<Device[]>('devices'),pools=useResource<Pool[]>('proxy-pools');
 const action=useAction();
 const [dialogOpen,setDialogOpen]=useState(false);
 const [editingEndpoint,setEditingEndpoint]=useState<Endpoint|null>(null);
 const [remove,setRemove]=useState<Endpoint>();
 const [probe,setProbe]=useState<string>('');
 const [probeTarget,setProbeTarget]=useState<Endpoint>();
 const [probePassword,setProbePassword]=useState('');

 async function submit(event:FormEvent<HTMLFormElement>){
  event.preventDefault();
  const values=Object.fromEntries(new FormData(event.currentTarget));
  const target=String(values.target??'');
  const body={
    name:String(values.name),
    bindAddress:String(values.bindAddress),
    port:Number(values.port),
    protocol:String(values.protocol),
    deviceId:target.startsWith('device:')?target.slice(7):null,
    poolId:target.startsWith('pool:')?target.slice(5):null,
    maxConnections:Number(values.maxConnections||64),
    enabled:editingEndpoint?values.enabled==='on':true,
    username:values.username?String(values.username):null,
    password:values.password?String(values.password):null,
    allowedClients:values.allowedClients?String(values.allowedClients):null
  };
  const success=await action.run(
    'form',
    ()=>api(editingEndpoint?`proxies/${editingEndpoint.id}`:'proxies',editingEndpoint?'PUT':'POST',body),
    editingEndpoint?'Đã cập nhật proxy.':'Đã tạo proxy.'
  );
  if(success)setDialogOpen(false);
 }

 function openCreate(){
  setEditingEndpoint(null);
  setDialogOpen(true);
 }

 function openEdit(p:Endpoint){
  setEditingEndpoint(p);
  setDialogOpen(true);
 }

 const currentTarget=editingEndpoint?(editingEndpoint.deviceId?`device:${editingEndpoint.deviceId}`:(editingEndpoint.poolId?`pool:${editingEndpoint.poolId}`:'')):'';

 return <Frame title="Proxy" description="Cấu hình listener TCP và kiểm tra đường ra thực tế." action={<Button id="create-proxy" variant="contained" startIcon={<Add/>} onClick={openCreate}>Tạo proxy</Button>}>{action.feedback}<ErrorNotice error={data.error}/>{probe&&<Alert onClose={()=>setProbe('')} severity="info">{probe}</Alert>}<Paper variant="outlined" sx={{flex:1,display:'flex',flexDirection:'column',minHeight:400,overflow:'hidden'}}><TableContainer sx={{flex:1,overflow:'auto'}}><Table sx={{minWidth:720}}><TableHead><TableRow>{['Tên / địa chỉ','Giao thức','Trạng thái','Thao tác'].map(x=><TableCell key={x} sx={{whiteSpace:'nowrap'}}>{x}</TableCell>)}</TableRow></TableHead><TableBody>{data.data?.map(p=><TableRow key={p.id}><TableCell><Typography sx={{fontWeight:600}}>{p.name}</Typography><Typography variant="body2" className="mono">{p.bindAddress}:{p.port}</Typography></TableCell><TableCell sx={{whiteSpace:'nowrap'}}>{p.protocol}<Typography variant="caption" sx={{display:"block"}}>{p.username?'Có xác thực':'Không xác thực'}</Typography></TableCell><TableCell sx={{whiteSpace:'nowrap'}}><Chip size="small" label={p.running?'Đang nghe':'Đã dừng'} color={p.running?'success':'default'}/></TableCell><TableCell><Stack direction="row" sx={{flexWrap:"nowrap",gap:1}}><Button id={`proxy-runtime-${p.id}`} size="small" disabled={action.busy.includes(p.id)} startIcon={p.running?<Stop/>:<PlayArrow/>} onClick={()=>void action.run(p.id,()=>api(`proxy-runtime/${p.id}`,p.running?'DELETE':'PUT'),p.running?'Đã gửi yêu cầu dừng.':'Listener đã khởi động.')}>{p.running?'Dừng':'Chạy'}</Button><Button size="small" onClick={()=>openEdit(p)}>Sửa</Button><Button size="small" disabled={action.busy.includes(p.id)} onClick={()=>{setProbeTarget(p);setProbePassword("")}}>Kiểm tra</Button><IconButton aria-label={`Sao chép địa chỉ ${p.name}`} onClick={()=>void action.run(`copy-${p.id}`,()=>navigator.clipboard.writeText(`${p.bindAddress}:${p.port}`),'Đã sao chép địa chỉ trên máy chạy service.')}><ContentCopy fontSize="small"/></IconButton><IconButton aria-label={`Xóa ${p.name}`} onClick={()=>setRemove(p)}><DeleteOutlined fontSize="small"/></IconButton></Stack></TableCell></TableRow>)}</TableBody></Table></TableContainer>{data.isPending?<Empty><CircularProgress/></Empty>:data.data?.length===0&&<Empty>Chưa có proxy. Thêm thiết bị hoặc pool trước khi tạo listener.</Empty>}</Paper><Dialog open={dialogOpen} slotProps={{transition:{onExited:()=>setEditingEndpoint(null)}}} onClose={()=>{if(!action.busy.includes('form'))setDialogOpen(false)}} fullWidth maxWidth="sm"><Box component="form" key={editingEndpoint?editingEndpoint.id:'create'} onSubmit={submit}><DialogTitle>{editingEndpoint?`Sửa proxy: ${editingEndpoint.name}`:'Tạo proxy mới'}</DialogTitle><DialogContent><Stack spacing={2} sx={{pt:1}}><ErrorNotice error={action.error}/><TextField id="proxy-name" name="name" label="Tên proxy" defaultValue={editingEndpoint?.name??''} required slotProps={{htmlInput:{maxLength:128}}}/><TextField name="bindAddress" label="Địa chỉ IP (Bind Address)" defaultValue={editingEndpoint?.bindAddress??'127.0.0.1'} required helperText="127.0.0.1 (chỉ máy này) hoặc 0.0.0.0 (mở cho toàn mạng LAN)."/><TextField name="port" label="Cổng (Port)" type="number" defaultValue={editingEndpoint?.port??1080} required slotProps={{htmlInput:{min:1,max:65535}}}/><TextField name="protocol" label="Giao thức" select defaultValue={editingEndpoint?.protocol??'Socks5'}><MenuItem value="Socks5">SOCKS5 TCP</MenuItem><MenuItem value="HttpConnect">HTTP / CONNECT</MenuItem></TextField><TextField name="target" label="Nguồn kết nối (Điện thoại hoặc Pool)" select defaultValue={currentTarget} required>{devices.data?.map(d=><MenuItem key={d.id} value={`device:${d.id}`}>{d.name} · Điện thoại đơn lẻ</MenuItem>)}{pools.data?.map(p=><MenuItem key={p.id} value={`pool:${p.id}`}>{p.name} · Pool chia tải</MenuItem>)}</TextField><TextField name="maxConnections" label="Kết nối tối đa" type="number" defaultValue={editingEndpoint?.maxConnections??64} slotProps={{htmlInput:{min:1,max:65535}}}/><FormControlLabel label="Cho phép hoạt động" control={<Switch name="enabled" defaultChecked={editingEndpoint?editingEndpoint.enabled:true}/>}/><TextField name="username" label="Tên đăng nhập proxy (tùy chọn)" defaultValue={editingEndpoint?.username??''} autoComplete="off"/><TextField name="password" label="Mật khẩu proxy" type="password" autoComplete="new-password" helperText={editingEndpoint?'Để trống nếu giữ nguyên mật khẩu cũ. Đổi mới: ít nhất 12 ký tự.':'Nếu bật xác thực: ít nhất 12 ký tự.'}/><TextField name="allowedClients" label="IP client được phép kết nối" defaultValue={editingEndpoint?.allowedClients??''} helperText="Danh sách IP cách nhau bằng dấu phẩy. Để trống nếu cho phép tất cả."/></Stack></DialogContent><DialogActions><Button onClick={()=>setDialogOpen(false)} disabled={action.busy.includes('form')}>Hủy</Button><Button id="save-proxy" type="submit" variant="contained" disabled={action.busy.includes('form')}>{editingEndpoint?'Lưu thay đổi':'Tạo proxy'}</Button></DialogActions></Box></Dialog><Dialog open={!!remove} onClose={()=>setRemove(undefined)}><DialogTitle>Xóa proxy {remove?.name}?</DialogTitle><DialogContent>Listener và các phiên đang chạy sẽ dừng. Thao tác không thể hoàn tác.</DialogContent><DialogActions><Button onClick={()=>setRemove(undefined)}>Giữ lại</Button><Button color="error" disabled={!!remove&&action.busy.includes(remove.id)} onClick={()=>{if(remove)void action.run(remove.id,()=>api(`proxies/${remove.id}`,'DELETE'),'Đã xóa proxy.').then(ok=>{if(ok)setRemove(undefined)})}}>Xóa proxy</Button></DialogActions></Dialog><Dialog open={!!probeTarget} onClose={()=>{setProbeTarget(undefined);setProbePassword('')}} fullWidth maxWidth="sm"><Box component="form" onSubmit={event=>{event.preventDefault();if(!probeTarget)return;const target=probeTarget;void action.run(`probe-${target.id}`,async()=>{const result=await api<{reachable:boolean;status:string;resolvedWanIp:string;latencyMs:number}>(`proxies/${target.id}/test`,'POST',{username:target.username,password:probePassword||null});setProbe(`${target.name}: ${result.status}; IP: ${result.resolvedWanIp||'chưa xác minh'}; ${result.latencyMs} ms`);setProbeTarget(undefined);setProbePassword('')})}}><DialogTitle>Kiểm tra {probeTarget?.name}</DialogTitle><DialogContent><Typography sx={{mb:2}}>Gửi HTTPS qua proxy và đo IP đường ra. Mật khẩu chỉ dùng cho lần kiểm tra này.</Typography>{probeTarget?.username&&<TextField fullWidth autoFocus type="password" label={`Mật khẩu cho ${probeTarget.username}`} value={probePassword} onChange={event=>setProbePassword(event.target.value)} autoComplete="off" required/>}</DialogContent><DialogActions><Button onClick={()=>{setProbeTarget(undefined);setProbePassword('')}}>Hủy</Button><Button type="submit" variant="contained" disabled={!!probeTarget&&action.busy.includes(`probe-${probeTarget.id}`)}>Kiểm tra đường ra</Button></DialogActions></Box></Dialog></Frame>
}
function Pools(){
 const data=useResource<Pool[]>('proxy-pools'),devices=useResource<Device[]>('devices');
 const action=useAction();
 const [dialogOpen,setDialogOpen]=useState(false);
 const [editingPool,setEditingPool]=useState<Pool|null>(null);
 const [removePool,setRemovePool]=useState<Pool>();

 async function submit(event:FormEvent<HTMLFormElement>){
  event.preventDefault();
  const form=new FormData(event.currentTarget);
  const members=form.getAll('members').map(deviceId=>({deviceId,weight:Number(form.get(`weight-${deviceId}`)??1),priority:Number(form.get(`priority-${deviceId}`)??0)}));
  const body={name:form.get('name'),strategy:form.get('strategy'),stickyTtlSeconds:Number(form.get('ttl')),enabled:form.get('enabled')==='on',members};
  if(await action.run('pool-form',()=>api(editingPool?`proxy-pools/${editingPool.id}`:'proxy-pools',editingPool?'PUT':'POST',body),'Đã lưu pool.')){
    setDialogOpen(false);
  }
 }

 function openCreate(){
  setEditingPool(null);
  setDialogOpen(true);
 }

 function openEdit(p:Pool){
  setEditingPool(p);
  setDialogOpen(true);
 }

 return <Frame title="Pool" description="Phân phối kết nối mới giữa các thiết bị đủ điều kiện." action={<Button id="create-pool" variant="contained" startIcon={<Add/>} onClick={openCreate}>Tạo pool</Button>}>{action.feedback}<ErrorNotice error={data.error}/><Stack spacing={2}>{data.data?.map(p=><Paper key={p.id} variant="outlined" sx={{p:3}}><Stack direction="row" sx={{alignItems:"center",justifyContent:"space-between"}}><Box><Typography variant="h6">{p.name}</Typography><Typography color="text.secondary">{STRATEGIES.find(s=>s.value===p.strategy)?.label ?? p.strategy} · Sticky {p.stickyTtlSeconds} giây · {p.members.length} thiết bị</Typography></Box><Stack direction="row"><Button onClick={()=>openEdit(p)}>Cấu hình</Button><Button color="error" onClick={()=>setRemovePool(p)}>Xóa</Button></Stack></Stack><Stack direction="row" sx={{gap:1,flexWrap:"wrap",mt:2}}>{p.members.map(m=><Chip key={m.deviceId} label={`${devices.data?.find(d=>d.id===m.deviceId)?.name??m.deviceId} · weight ${m.weight}`} variant="outlined"/>)}</Stack></Paper>)}</Stack>{data.data?.length===0&&<Paper variant="outlined"><Empty>Chưa có pool. Thêm thiết bị để phân phối kết nối.</Empty></Paper>}<Dialog fullWidth maxWidth="sm" open={dialogOpen} slotProps={{transition:{onExited:()=>setEditingPool(null)}}} onClose={()=>setDialogOpen(false)}><Box component="form" key={editingPool?editingPool.id:'create'} onSubmit={submit}><DialogTitle>{editingPool?'Cấu hình pool':'Tạo pool'}</DialogTitle><DialogContent><Stack spacing={2} sx={{pt:1}}><ErrorNotice error={action.error}/><TextField name="name" label="Tên pool" required defaultValue={editingPool?.name??''} disabled={!!editingPool}/><TextField name="strategy" label="Cách chia kết nối vào điện thoại" select defaultValue={editingPool?.strategy??'RoundRobin'}>{STRATEGIES.map(s=><MenuItem key={s.value} value={s.value}><Box><Typography variant="body2" sx={{fontWeight:600}}>{s.label}</Typography><Typography variant="caption" color="text.secondary" sx={{display:'block'}}>{s.desc}</Typography></Box></MenuItem>)}</TextField><TextField name="ttl" type="number" label="Sticky TTL (giây)" defaultValue={editingPool?.stickyTtlSeconds??0} slotProps={{htmlInput:{min:0,max:86400}}} helperText="0 để tắt. Hiện backend nhận diện client theo IP nguồn."/><FormControlLabel label="Cho phép pool" control={<Switch name="enabled" defaultChecked={editingPool?.enabled??true}/>}/>{devices.data?.map(d=><Stack direction="row" key={d.id} sx={{gap:2,alignItems:"center"}}><FormControlLabel sx={{flex:1}} label={d.name} control={<Switch name="members" value={d.id} defaultChecked={!!editingPool?.members.some(m=>m.deviceId===d.id)}/>}/><TextField size="small" sx={{width:110}} name={`weight-${d.id}`} label="Trọng số" type="number" defaultValue={editingPool?.members.find(m=>m.deviceId===d.id)?.weight??1} slotProps={{htmlInput:{min:1,max:10000}}}/><TextField size="small" sx={{width:110}} name={`priority-${d.id}`} label="Ưu tiên" type="number" defaultValue={editingPool?.members.find(m=>m.deviceId===d.id)?.priority??0} slotProps={{htmlInput:{min:0,max:10000}}}/></Stack>)}</Stack></DialogContent><DialogActions><Button onClick={()=>setDialogOpen(false)}>Hủy</Button><Button type="submit" variant="contained" disabled={action.busy.includes('pool-form')}>Lưu</Button></DialogActions></Box></Dialog><Dialog open={!!removePool} onClose={()=>setRemovePool(undefined)}><DialogTitle>Xóa pool {removePool?.name}?</DialogTitle><DialogContent>Không thể hoàn tác. Pool đang được proxy sử dụng sẽ không bị xóa.</DialogContent><DialogActions><Button onClick={()=>setRemovePool(undefined)}>Giữ lại</Button><Button color="error" disabled={action.busy.includes("delete-pool")} onClick={()=>{if(removePool)void action.run("delete-pool",()=>api(`proxy-pools/${removePool.id}`,"DELETE"),"Đã xóa pool.").then(ok=>{if(ok)setRemovePool(undefined)})}}>Xóa pool</Button></DialogActions></Dialog></Frame>
}
function Rotations(){const data=useResource<Rotation[]>('rotation/history'),devices=useResource<Device[]>('devices');const action=useAction();const [selected,setSelected]=useState<Device>();return <Frame title="Đổi IP" description="Reconnect cellular rồi đo lại egress. Nhà mạng có thể giữ nguyên IP.">{action.feedback}<ErrorNotice error={data.error}/><Paper variant="outlined" sx={{p:3}}><Typography variant="h6" sx={{mb:2}}>Kết nối lại từng thiết bị</Typography><Stack direction="row" sx={{gap:2,flexWrap:"wrap"}}>{devices.data?.map(d=><Button key={d.id} variant="outlined" disabled={action.busy.includes(d.id)||d.actualState!=='ProxyReady'} onClick={()=>setSelected(d)}>{d.name}</Button>)}</Stack>{devices.data?.length===0&&<Typography color="text.secondary">Chưa có thiết bị để thao tác.</Typography>}</Paper><Paper variant="outlined" sx={{flex:1,display:'flex',flexDirection:'column',minHeight:360,overflow:'hidden'}}><TableContainer sx={{flex:1,overflow:'auto'}}><Table sx={{minWidth:720}}><TableHead><TableRow>{['Thời điểm','IP trước','IP sau','Kết quả','Thời lượng'].map(s=><TableCell key={s} sx={{whiteSpace:'nowrap'}}>{s}</TableCell>)}</TableRow></TableHead><TableBody>{data.data?.map((r,i)=><TableRow key={`${r.changedAt}-${i}`}><TableCell sx={{whiteSpace:'nowrap'}}>{date(r.changedAt)}</TableCell><TableCell className="mono" sx={{whiteSpace:'nowrap'}}>{r.previousIp||'Chưa biết'}</TableCell><TableCell className="mono" sx={{whiteSpace:'nowrap'}}>{r.currentIp||'Chưa đo được'}</TableCell><TableCell sx={{maxWidth:360,overflow:'hidden',textOverflow:'ellipsis',whiteSpace:'nowrap'}} title={r.status}>{r.status}</TableCell><TableCell sx={{whiteSpace:'nowrap'}}>{r.duration}</TableCell></TableRow>)}</TableBody></Table></TableContainer>{data.data?.length===0&&<Empty>Chưa có lịch sử reconnect.</Empty>}</Paper><Dialog open={!!selected} onClose={()=>setSelected(undefined)}><DialogTitle>Kết nối lại {selected?.name}?</DialogTitle><DialogContent>Các kết nối qua thiết bị này có thể bị ngắt. IP mới phụ thuộc nhà mạng. Không tự chuyển socket đang chạy sang máy khác.</DialogContent><DialogActions><Button onClick={()=>setSelected(undefined)}>Hủy</Button><Button variant="contained" onClick={()=>{if(selected){const d=selected;setSelected(undefined);void action.run(d.id,()=>api('rotation/rotate','POST',{deviceId:d.id}),'Đã nhận kết quả reconnect. Xem lịch sử để biết IP có thay đổi.')}}}>Kết nối lại</Button></DialogActions></Dialog></Frame>}
function Sessions(){const [page,setPage]=useState(0);const [endpoint,setEndpoint]=useState('');const [device,setDevice]=useState('');const endpoints=useResource<Endpoint[]>('proxies');const devices=useResource<Device[]>('devices');const data=useResource<{items:Session[];totalCount:number}>(`proxy-sessions?page=${page+1}&pageSize=25${endpoint?`&endpointId=${endpoint}`:""}${device?`&deviceId=${device}`:""}`);return <Frame title="Phiên kết nối" description="Các phiên đã kết thúc. Số byte là payload được MobiFlux quan sát."><Stack direction={{xs:"column",sm:"row"}} spacing={2}><TextField select label="Proxy" value={endpoint} onChange={e=>{setEndpoint(e.target.value);setPage(0)}} sx={{minWidth:200}}><MenuItem value="">Tất cả proxy</MenuItem>{endpoints.data?.map(p=><MenuItem key={p.id} value={p.id}>{p.name}</MenuItem>)}</TextField><TextField select label="Thiết bị" value={device} onChange={e=>{setDevice(e.target.value);setPage(0)}} sx={{minWidth:200}}><MenuItem value="">Tất cả thiết bị</MenuItem>{devices.data?.map(d=><MenuItem key={d.id} value={d.id}>{d.name}</MenuItem>)}</TextField><Button onClick={()=>void data.refetch()}>Làm mới</Button></Stack><ErrorNotice error={data.error}/><Paper variant="outlined" sx={{flex:1,display:'flex',flexDirection:'column',minHeight:400,overflow:'hidden'}}><TableContainer sx={{flex:1,overflow:'auto'}}><Table sx={{minWidth:720}}><TableHead><TableRow>{['Đích','Bắt đầu','Tải lên','Tải xuống','Kết thúc'].map(s=><TableCell key={s} sx={{whiteSpace:'nowrap'}}>{s}</TableCell>)}</TableRow></TableHead><TableBody>{data.data?.items.map(s=><TableRow key={s.id}><TableCell className="mono" sx={{whiteSpace:'nowrap',maxWidth:260,overflow:'hidden',textOverflow:'ellipsis'}} title={`${s.destinationHost}:${s.destinationPort}`}>{s.destinationHost}:{s.destinationPort}</TableCell><TableCell sx={{whiteSpace:'nowrap'}}>{date(s.startedAt)}</TableCell><TableCell sx={{whiteSpace:'nowrap'}}>{formatBytes(s.bytesUp)}</TableCell><TableCell sx={{whiteSpace:'nowrap'}}>{formatBytes(s.bytesDown)}</TableCell><TableCell sx={{maxWidth:240,overflow:'hidden',textOverflow:'ellipsis',whiteSpace:'nowrap'}} title={s.closeReason}>{s.closeReason}</TableCell></TableRow>)}</TableBody></Table></TableContainer>{data.data?.items.length===0&&<Empty>Chưa ghi nhận phiên đã kết thúc.</Empty>}<TablePagination component="div" count={data.data?.totalCount??0} page={page} rowsPerPage={25} rowsPerPageOptions={[25]} onPageChange={(_,p)=>setPage(p)} labelDisplayedRows={({from,to,count})=>`${from}–${to} / ${count}`}/></Paper></Frame>}
function SettingsPage(){const action=useAction();return <Frame title="Thiết lập kết nối" description="Web dùng cùng service và dữ liệu với desktop."><Paper variant="outlined" sx={{p:3}}><Typography variant="h6">Máy chủ hiện tại</Typography><Button id="database-backup" disabled={action.busy.includes("backup")} onClick={()=>void action.run("backup",async()=>{const response=await fetch("/api/v1/mobiflux/maintenance/backup",{method:"POST",signal:AbortSignal.timeout(90000),credentials:"same-origin"});if(!response.ok)throw new Error("Không tạo được bản sao lưu. Kiểm tra dịch vụ rồi thử lại.");const blob=await response.blob();const url=URL.createObjectURL(blob);const link=document.createElement("a");link.href=url;link.download=`mobiflux-${new Date().toISOString().slice(0,10)}.db`;link.click();setTimeout(()=>URL.revokeObjectURL(url),60000)},"Đã tạo bản sao lưu. Kiểm tra thư mục tải xuống.")}>Tải bản sao lưu dữ liệu</Button><Typography variant="body2" color="text.secondary">Bản sao chứa cấu hình và lịch sử đã lưu, không gồm phiên chưa ghi xuống đĩa. Giữ tệp ở nơi riêng tư.</Typography><Typography className="mono" sx={{my:2,overflowWrap:'anywhere'}}>{location.origin}</Typography><Typography color="text.secondary">Đóng tab web không dừng proxy. Khi service dừng, mở lại từ desktop hoặc Windows Service; trình duyệt không thể tự khởi động backend đã tắt.</Typography></Paper><Alert severity="warning">Chỉ dùng local trong cấu hình hiện tại. Chưa mở quản trị LAN: cần xác thực, HTTPS và thiết lập trust trước khi truy cập từ iPhone.</Alert><Alert severity="info">SOCKS5 / HTTP hỗ trợ TCP. Không phải VPN toàn hệ thống; không tự chuyển traffic của ứng dụng chưa cấu hình proxy.</Alert></Frame>}
export default function App(){return <QueryClientProvider client={client}><BrowserRouter><Shell/></BrowserRouter></QueryClientProvider>}









function Networking(){
 const interfaces=useResource<{id:string;name:string;status:string;ipAddresses:string;speedMbps:number}[]>('networking/interfaces')
 const connections=useResource<{localAddress:string;localPort:number;remoteAddress:string;remotePort:number;state:string}[]>('networking/netstat')
 const [host,setHost]=useState('1.1.1.1')
 const [result,setResult]=useState<string>()
 const action=useAction()
 return <Frame title="Mạng Windows" description="Chẩn đoán kết nối máy chủ Windows và các phiên TCP đang hoạt động (Netstat)." action={<Button onClick={()=>{void interfaces.refetch();void connections.refetch()}}>Làm mới</Button>}><ErrorNotice error={interfaces.error??connections.error}/><Paper variant="outlined" sx={{p:2}}><Box component="form" onSubmit={event=>{event.preventDefault();void action.run('ping',async()=>{const response=await api<{host:string;status:string;roundTripTimeMs:number}>('networking/ping','POST',{host,timeoutMs:3000});setResult(`${response.host}: ${response.status} · ${response.roundTripTimeMs} ms`)})}}><Stack direction={{xs:'column',sm:'row'}} spacing={1}><TextField id="ping-host" size="small" label="Host hoặc IP" value={host} onChange={event=>setHost(event.target.value)} required slotProps={{htmlInput:{maxLength:253}}}/><Button id="ping-run" type="submit" disabled={action.busy.includes('ping')}>Ping</Button></Stack></Box>{result&&<Typography sx={{mt:2}}>{result}</Typography>}</Paper><Paper variant="outlined" sx={{mb:2,p:2,overflow:'hidden'}}><Typography variant="subtitle1" sx={{fontWeight:600,mb:1}}>Card mạng Windows</Typography><TableContainer sx={{maxHeight:240,overflow:'auto'}}><Table size="small" sx={{minWidth:500}}><TableHead><TableRow>{['Giao diện','Trạng thái','Địa chỉ IP'].map(label=><TableCell key={label} sx={{whiteSpace:'nowrap'}}>{label}</TableCell>)}</TableRow></TableHead><TableBody>{interfaces.data?.map(item=><TableRow key={item.id}><TableCell sx={{whiteSpace:'nowrap'}}>{item.name}</TableCell><TableCell sx={{whiteSpace:'nowrap'}}>{item.status}</TableCell><TableCell className="mono" sx={{whiteSpace:'nowrap',maxWidth:350,overflow:'hidden',textOverflow:'ellipsis'}} title={item.ipAddresses}>{item.ipAddresses}</TableCell></TableRow>)}</TableBody></Table></TableContainer></Paper><Paper variant="outlined" sx={{flex:1,display:'flex',flexDirection:'column',minHeight:300,p:2,overflow:'hidden'}}><Typography variant="subtitle1" sx={{fontWeight:600,mb:1}}>Kết nối TCP trên Windows (Active Netstat)</Typography><Typography variant="caption" color="text.secondary" sx={{mb:1,display:'block'}}>Các socket TCP đang mở trên Windows để giám sát client kết nối vào cổng proxy hoặc service.</Typography><TableContainer sx={{flex:1,overflow:'auto'}}><Table stickyHeader size="small" sx={{minWidth:500}}><TableHead><TableRow>{['Local','Remote','Trạng thái'].map(label=><TableCell key={label} sx={{whiteSpace:'nowrap'}}>{label}</TableCell>)}</TableRow></TableHead><TableBody>{connections.data?.map((item,index)=><TableRow key={index}><TableCell className="mono" sx={{whiteSpace:'nowrap'}} title={`${item.localAddress}:${item.localPort}`}>{item.localAddress}:{item.localPort}</TableCell><TableCell className="mono" sx={{whiteSpace:'nowrap',maxWidth:300,overflow:'hidden',textOverflow:'ellipsis'}} title={`${item.remoteAddress}:${item.remotePort}`}>{item.remoteAddress}:{item.remotePort}</TableCell><TableCell sx={{whiteSpace:'nowrap'}}>{item.state}</TableCell></TableRow>)}</TableBody></Table></TableContainer>{connections.data?.length===0&&<Empty>Chưa có kết nối TCP nào.</Empty>}</Paper></Frame>
}
