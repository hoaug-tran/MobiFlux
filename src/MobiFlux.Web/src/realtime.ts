import { useEffect, useState } from 'react'
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import type { Runtime } from './api'
export function useRuntime(resync:()=>void){
 const [sample,setSample]=useState<Runtime>()
 const [history,setHistory]=useState<Runtime[]>([])
 const [state,setState]=useState('Đang kết nối')
 const [received,setReceived]=useState(0)
 const [now,setNow]=useState(()=>Date.now())
 useEffect(()=>{
  let disposed=false
  let retry:ReturnType<typeof setTimeout>|undefined
  const connection=new HubConnectionBuilder().withUrl('/hubs/runtime').withAutomaticReconnect([0,2000,5000,10000]).configureLogging(LogLevel.Error).build()
  connection.on('RuntimeUpdated',(value:Runtime)=>{if(!disposed){setSample(value);setHistory(previous=>[...(previous.at(-1) && (value.bytesUp < previous.at(-1)!.bytesUp || value.bytesDown < previous.at(-1)!.bytesDown) ? [] : previous).filter(item=>Date.parse(value.at)-Date.parse(item.at)<900000),value].slice(-1800));setReceived(Date.now());setState('Trực tiếp')}})
  async function start(){try{await connection.start();if(disposed){await connection.stop();return}resync()}catch{if(!disposed){setState('Mất kết nối trực tiếp');retry=setTimeout(start,5000)}}}
  connection.onreconnecting(()=>setState('Đang kết nối lại'))
  connection.onreconnected(()=>{setReceived(0);setState('Đang đồng bộ');resync()})
  connection.onclose(()=>{if(!disposed){setState('Mất kết nối trực tiếp');retry=setTimeout(start,5000)}})
  void start()
  const timer=setInterval(()=>setNow(Date.now()),2000)
  return()=>{disposed=true;clearTimeout(retry);clearInterval(timer);void connection.stop()}
 },[resync])
 return {sample,history,state:received&&now-received>10000?'Dữ liệu đã cũ':state,received}
}



