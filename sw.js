const CACHE='neon-oink-v19';
const ASSETS=['/','/index.html','/manifest.json','/assets/splat/gaussian-splats-3d.module.js?v=2','/assets/splat/scenery.splat','https://cdn.jsdelivr.net/npm/three@0.180.0/build/three.module.js','https://cdn.jsdelivr.net/npm/three@0.180.0/examples/jsm/environments/RoomEnvironment.js','https://cdn.jsdelivr.net/npm/three@0.180.0/examples/jsm/webxr/VRButton.js'];
self.addEventListener('install',e=>{e.waitUntil(caches.open(CACHE).then(c=>c.addAll(ASSETS)).then(()=>self.skipWaiting()));});
self.addEventListener('activate',e=>{e.waitUntil(caches.keys().then(ks=>Promise.all(ks.filter(k=>k!==CACHE).map(k=>caches.delete(k)))).then(()=>self.clients.claim()));});
self.addEventListener('fetch',e=>{
  const url=new URL(e.request.url);
  if(e.request.mode==='navigate'){
    e.respondWith(fetch(e.request).then(r=>{const cp=r.clone();caches.open(CACHE).then(c=>c.put('/index.html',cp));return r;}).catch(()=>caches.match('/index.html')));
    return;
  }
  e.respondWith(caches.match(e.request).then(r=>{
    if(r)return r;
    return fetch(e.request).then(nr=>{
      if(!nr||nr.status!==200||nr.method!=='GET')return nr;
      const cp=nr.clone();
      caches.open(CACHE).then(c=>c.put(e.request,cp));
      return nr;
    });
  }).catch(()=>new Response('Offline',{status:503})));
});
