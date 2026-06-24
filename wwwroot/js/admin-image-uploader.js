(() => {
  const fmt = b => b ? `${(b / 1024 / 1024).toFixed(2)} MB` : '';
  const token = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
  const splitUrls = value => (value || '').split(/\r?\n|,|;/).map(x => x.trim()).filter(Boolean);
  const uniqueUrls = urls => [...new Map(urls.filter(Boolean).map(u => [u.toLowerCase(), u])).values()];
  const dispatchInput = el => el?.dispatchEvent(new Event('input', { bubbles: true }));
  const shortUrl = url => { try { const p = new URL(url); return decodeURIComponent((p.pathname.split('/').pop() || p.host)); } catch { return url.length > 46 ? `${url.slice(0, 24)}…${url.slice(-18)}` : url; } };
  const esc = value => String(value || '').replace(/[&<>"']/g, ch => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[ch]));

  function getGallery(textarea){ return uniqueUrls(splitUrls(textarea?.value)); }
  function setGallery(textarea, urls){ if(!textarea) return; textarea.value = uniqueUrls(urls).join('\n'); dispatchInput(textarea); }
  function addUrl(textarea, url){ setGallery(textarea, [...getGallery(textarea), url]); }

  function renderProductManager(root){
    const primary = document.querySelector(root.dataset.primaryTarget);
    const gallery = document.querySelector(root.dataset.galleryTarget);
    const hero = root.querySelector('[data-aim-primary]');
    const grid = root.querySelector('[data-aim-gallery]');
    const render = () => {
      if(!primary || !gallery || !hero || !grid) return;
      const primaryUrl = primary.value.trim();
      const urls = getGallery(gallery);
      if (gallery.value !== urls.join('\n')) gallery.value = urls.join('\n');
      hero.innerHTML = primaryUrl ? `<div class="admin-image-card admin-image-thumb is-primary"><img src="${esc(primaryUrl)}" alt="Ảnh đại diện" onerror="this.onerror=null;this.src='/images/no-image.png'"><span class="admin-image-badge"><i class="bi bi-star-fill"></i> Đại diện</span><div class="admin-image-actions"><button type="button" class="btn btn-light btn-sm" title="Xóa ảnh đại diện" data-clear-primary><i class="bi bi-trash"></i></button></div></div>` : `<button type="button" class="admin-image-add-tile admin-image-primary-empty" data-add-image><i class="bi bi-plus-lg"></i><span>Thêm ảnh đại diện</span></button>`;
      const addTile = `<button type="button" class="admin-image-add-tile" data-add-image><i class="bi bi-plus-lg"></i><span>Thêm ảnh</span></button>`;
      grid.innerHTML = `${urls.map(u => `<div class="admin-image-card admin-image-thumb"><img src="${esc(u)}" alt="Ảnh thư viện" onerror="this.onerror=null;this.src='/images/no-image.png'"><span class="admin-image-badge is-gallery">Thư viện</span><div class="admin-image-actions"><button type="button" class="btn btn-light btn-sm" title="Đặt làm ảnh đại diện" data-make-primary="${encodeURIComponent(u)}"><i class="bi bi-star"></i></button><button type="button" class="btn btn-light btn-sm text-danger" title="Xóa" data-remove-url="${encodeURIComponent(u)}"><i class="bi bi-trash"></i></button></div></div>`).join('')}${addTile}`;
    };
    root.addEventListener('click', e => {
      const clear = e.target.closest('[data-clear-primary]');
      const make = e.target.closest('[data-make-primary]');
      const remove = e.target.closest('[data-remove-url]');
      const add = e.target.closest('[data-add-image]');
      if(add){ const uploader = document.querySelector(`[data-admin-image-uploader][data-primary-target="${root.dataset.primaryTarget}"]`); uploader?.querySelector('[data-aiu-file]')?.click(); }
      if(clear){ primary.value=''; dispatchInput(primary); }
      if(make){ const next = decodeURIComponent(make.dataset.makePrimary); const old = primary.value.trim(); primary.value = next; if(old && old.toLowerCase() !== next.toLowerCase()) addUrl(gallery, old); dispatchInput(primary); }
      if(remove){ const url = decodeURIComponent(remove.dataset.removeUrl); setGallery(gallery, getGallery(gallery).filter(x => x.toLowerCase() !== url.toLowerCase())); if(primary.value.trim().toLowerCase() === url.toLowerCase()){ primary.value=''; dispatchInput(primary); } }
    });
    [primary, gallery].forEach(el => el?.addEventListener('input', render));
    render();
  }

  function renderSingleManager(root){
    const input = document.querySelector(root.dataset.primaryTarget);
    const box = root.querySelector('[data-aim-single]');
    const label = root.dataset.label || 'Ảnh';
    const render = () => { const url = input?.value?.trim(); if(!box) return; box.innerHTML = url ? `<div class="admin-image-card admin-image-thumb is-primary"><img src="${esc(url)}" alt="${esc(label)}" onerror="this.onerror=null;this.src='/images/no-image.png'"><span class="admin-image-badge">${esc(label)}</span><div class="admin-image-actions"><button type="button" class="btn btn-light btn-sm text-danger" title="Xóa" data-clear-single><i class="bi bi-trash"></i></button></div></div><button type="button" class="admin-image-add-tile" data-add-single><i class="bi bi-plus-lg"></i><span>Thay ảnh</span></button>` : `<button type="button" class="admin-image-add-tile" data-add-single><i class="bi bi-plus-lg"></i><span>Thêm ảnh</span></button>`; };
    root.addEventListener('click', e => { if(e.target.closest('[data-add-single]')){ document.querySelector(`[data-admin-image-uploader][data-primary-target="${root.dataset.primaryTarget}"]`)?.querySelector('[data-aiu-file]')?.click(); } if(e.target.closest('[data-clear-single]') && input){ input.value=''; dispatchInput(input); } });
    input?.addEventListener('input', render); render();
  }

  function initManagers(){ document.querySelectorAll('[data-admin-image-manager="product"]').forEach(renderProductManager); document.querySelectorAll('[data-admin-image-manager="single"]').forEach(renderSingleManager); }

  function setPreview(root, url){ const img=root.querySelector('[data-aiu-preview]'); if(img&&url){ img.src=url; img.classList.remove('d-none'); } }
  function init(root){
    const file=root.querySelector('[data-aiu-file]'), drop=root.querySelector('[data-aiu-drop]'), status=root.querySelector('[data-aiu-status]'), progress=root.querySelector('[data-aiu-progress]'), primary=document.querySelector(root.dataset.primaryTarget), gallery=root.dataset.galleryTarget ? document.querySelector(root.dataset.galleryTarget) : null;
    const name=root.querySelector('[data-aiu-name]'), size=root.querySelector('[data-aiu-size]'); const current=primary?.value?.trim(); if(current) setPreview(root,current);
    const handle = files => { if(!files?.length) return; [...files].forEach((f,i)=>upload(f, i===0)); };
    async function upload(f, first){ if(name) name.textContent=f.name; if(size) size.textContent=fmt(f.size); status.textContent='Đang upload...'; status.className='small text-primary'; progress.style.width='35%'; const fd=new FormData(); fd.append('file', f);
      try{ const res=await fetch('/AdminImageUploads/Upload',{method:'POST',headers:{'RequestVerificationToken':token()},body:fd}); const data=await res.json().catch(()=>({message:'Upload thất bại.'})); if(!res.ok||!data.success) throw new Error(data.message||'Upload thất bại.'); progress.style.width='100%'; status.textContent='Upload thành công'; status.className='small text-success'; setPreview(root,data.url); if(primary && !primary.value.trim()) { primary.value=data.url; dispatchInput(primary); } else if(gallery) addUrl(gallery,data.url); else if(primary) { primary.value=data.url; dispatchInput(primary); } }catch(e){ progress.style.width='100%'; status.textContent=e.message||'Upload thất bại'; status.className='small text-danger'; }
    }
    file?.addEventListener('change',()=>handle(file.files)); drop?.addEventListener('click',()=>file?.click()); ['dragenter','dragover'].forEach(ev=>drop?.addEventListener(ev,e=>{e.preventDefault();drop.classList.add('is-dragover')})); ['dragleave','drop'].forEach(ev=>drop?.addEventListener(ev,e=>{e.preventDefault();drop.classList.remove('is-dragover')})); drop?.addEventListener('drop',e=>handle(e.dataTransfer.files)); primary?.addEventListener('input',()=>setPreview(root,primary.value.trim()));
  }
  initManagers(); document.querySelectorAll('[data-admin-image-uploader]').forEach(init);
})();
