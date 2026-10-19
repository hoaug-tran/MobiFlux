import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'
export default defineConfig({plugins:[react()],server:{host:'127.0.0.1',proxy:{'/api':'http://127.0.0.1:5080','/health':'http://127.0.0.1:5080','/hubs':{target:'http://127.0.0.1:5080',ws:true}}},build:{outDir:'../MobiFlux.Service/wwwroot',emptyOutDir:true,rolldownOptions:{output:{codeSplitting:{groups:[{name:'material',test:/node_modules[\\/](@mui|@emotion)[\\/]/},{name:'react',test:/node_modules[\\/](react|react-dom|scheduler)[\\/]/},{name:'runtime',test:/node_modules[\\/]@microsoft[\\/]signalr[\\/]/}]}}}}})

