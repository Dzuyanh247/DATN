(() => {
  const fmt = b => b ? `${(b / 1024 / 1024).toFixed(2)} MB` : '';
  const token = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
  function addUrl(textarea, url){ const lines=(textarea.value||'').split(/\r?\n/).map(x=>x.trim()).filter(Boolean); if(!lines.some(x=>x.toLowerCase()===url.toLowerCase())) lines.push(url); textarea.value=lines.join('\n'); textarea.dispatchEvent(new Event('input',{bubbles:true})); }
  function setPreview(root, url){ const img=root.querySelector('[data-aiu-preview]'); if(img&&url){ img.src=url; img.classList.remove('d-none'); } }
  function init(root){
    const file=root.querySelector('[data-aiu-file]'), drop=root.querySelector('[data-aiu-drop]'), status=root.querySelector('[data-aiu-status]'), progress=root.querySelector('[data-aiu-progress]'), primary=document.querySelector(root.dataset.primaryTarget), gallery=root.dataset.galleryTarget ? document.querySelector(root.dataset.galleryTarget) : null;
    const name=root.querySelector('[data-aiu-name]'), size=root.querySelector('[data-aiu-size]');
    const current=primary?.value?.trim(); if(current) setPreview(root,current);
    const handle = files => { if(!files?.length) return; [...files].forEach((f,i)=>upload(f, i===0)); };
    async function upload(f, first){
      if(name) name.textContent=f.name; if(size) size.textContent=fmt(f.size); setPreview(root, URL.createObjectURL(f));
      status.textContent='Đang upload...'; status.className='small text-primary'; progress.style.width='35%';
      const fd=new FormData(); fd.append('file', f);
      try{ const res=await fetch('/AdminImageUploads/Upload',{method:'POST',headers:{'RequestVerificationToken':token()},body:fd}); const data=await res.json().catch(()=>({message:'Upload thất bại.'})); if(!res.ok||!data.success) throw new Error(data.message||'Upload thất bại.');
        progress.style.width='100%'; status.textContent='Upload thành công'; status.className='small text-success'; setPreview(root,data.url);
        if(primary && (!primary.value.trim() || first && !gallery)) { primary.value=data.url; primary.dispatchEvent(new Event('input',{bubbles:true})); }
        else if(gallery) addUrl(gallery,data.url);
      }catch(e){ progress.style.width='100%'; status.textContent=e.message||'Upload thất bại'; status.className='small text-danger'; }
    }
    file?.addEventListener('change',()=>handle(file.files)); drop?.addEventListener('click',()=>file?.click());
    ['dragenter','dragover'].forEach(ev=>drop?.addEventListener(ev,e=>{e.preventDefault();drop.classList.add('is-dragover')}));
    ['dragleave','drop'].forEach(ev=>drop?.addEventListener(ev,e=>{e.preventDefault();drop.classList.remove('is-dragover')}));
    drop?.addEventListener('drop',e=>handle(e.dataTransfer.files)); primary?.addEventListener('input',()=>setPreview(root,primary.value.trim()));
  }
  document.querySelectorAll('[data-admin-image-uploader]').forEach(init);
})();
