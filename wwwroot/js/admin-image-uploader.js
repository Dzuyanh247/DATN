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
      hero.innerHTML = primaryUrl ? `<div class="admin-image-card is-primary"><img src="${esc(primaryUrl)}" alt="Ảnh đại diện" onerror="this.onerror=null;this.src='/images/no-image.png'"><div class="admin-image-card-body"><span class="badge text-bg-warning">Ảnh đại diện</span><button type="button" class="btn btn-sm btn-outline-danger" data-clear-primary>Xóa</button><small class="text-muted text-truncate d-block" title="${esc(primaryUrl)}">${esc(shortUrl(primaryUrl))}</small></div></div>` : `<div class="admin-image-empty"><i class="bi bi-image"></i><strong>Chưa có ảnh đại diện</strong><span>Upload ảnh đầu tiên hoặc dán URL để hiển thị preview.</span></div>`;
      grid.innerHTML = urls.length ? urls.map(u => `<div class="admin-image-card"><img src="${esc(u)}" alt="Ảnh thư viện" onerror="this.onerror=null;this.src='/images/no-image.png'"><div class="admin-image-card-body"><span class="badge text-bg-info">Thư viện</span><button type="button" class="btn btn-sm btn-outline-primary" data-make-primary="${encodeURIComponent(u)}">Đặt làm ảnh đại diện</button><button type="button" class="btn btn-sm btn-outline-danger" data-remove-url="${encodeURIComponent(u)}">Xóa</button><small class="text-muted text-truncate d-block" title="${esc(u)}">${esc(shortUrl(u))}</small></div></div>`).join('') : `<div class="admin-image-empty"><i class="bi bi-images"></i><strong>Chưa có ảnh thư viện</strong><span>Upload thêm ảnh khi đã có ảnh đại diện để bổ sung vào thư viện.</span></div>`;
    };
    root.addEventListener('click', e => {
      const clear = e.target.closest('[data-clear-primary]');
      const make = e.target.closest('[data-make-primary]');
      const remove = e.target.closest('[data-remove-url]');
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
    const render = () => { const url = input?.value?.trim(); if(!box) return; box.innerHTML = url ? `<div class="admin-image-card is-primary"><img src="${esc(url)}" alt="${esc(label)}" onerror="this.onerror=null;this.src='/images/no-image.png'"><div class="admin-image-card-body"><span class="badge text-bg-warning">${esc(label)}</span><button type="button" class="btn btn-sm btn-outline-danger" data-clear-single>Xóa</button><small class="text-muted text-truncate d-block" title="${esc(url)}">${esc(shortUrl(url))}</small></div></div>` : `<div class="admin-image-empty"><i class="bi bi-image"></i><strong>Chưa có ${label.toLowerCase()}</strong><span>Dán URL hoặc upload từ thiết bị.</span></div>`; };
    root.addEventListener('click', e => { if(e.target.closest('[data-clear-single]') && input){ input.value=''; dispatchInput(input); } });
    input?.addEventListener('input', render); render();
  }

  function initManagers(){ document.querySelectorAll('[data-admin-image-manager="product"]').forEach(renderProductManager); document.querySelectorAll('[data-admin-image-manager="single"]').forEach(renderSingleManager); }

  function setPreview(root, url){ const img=root.querySelector('[data-aiu-preview]'); if(img&&url){ img.src=url; img.classList.remove('d-none'); } }
  function init(root){
    const file=root.querySelector('[data-aiu-file]'), drop=root.querySelector('[data-aiu-drop]'), status=root.querySelector('[data-aiu-status]'), progress=root.querySelector('[data-aiu-progress]'), primary=document.querySelector(root.dataset.primaryTarget), gallery=root.dataset.galleryTarget ? document.querySelector(root.dataset.galleryTarget) : null;
    const name=root.querySelector('[data-aiu-name]'), size=root.querySelector('[data-aiu-size]'); const current=primary?.value?.trim(); if(current) setPreview(root,current);
    const handle = files => { if(!files?.length) return; [...files].forEach((f,i)=>upload(f, i===0)); };
    async function upload(f, first){ if(name) name.textContent=f.name; if(size) size.textContent=fmt(f.size); setPreview(root, URL.createObjectURL(f)); status.textContent='Đang upload...'; status.className='small text-primary'; progress.style.width='35%'; const fd=new FormData(); fd.append('file', f);
      try{ const res=await fetch('/AdminImageUploads/Upload',{method:'POST',headers:{'RequestVerificationToken':token()},body:fd}); const data=await res.json().catch(()=>({message:'Upload thất bại.'})); if(!res.ok||!data.success) throw new Error(data.message||'Upload thất bại.'); progress.style.width='100%'; status.textContent='Upload thành công'; status.className='small text-success'; setPreview(root,data.url); if(primary && !primary.value.trim()) { primary.value=data.url; dispatchInput(primary); } else if(gallery) addUrl(gallery,data.url); else if(primary) { primary.value=data.url; dispatchInput(primary); } }catch(e){ progress.style.width='100%'; status.textContent=e.message||'Upload thất bại'; status.className='small text-danger'; }
    }
    file?.addEventListener('change',()=>handle(file.files)); drop?.addEventListener('click',()=>file?.click()); ['dragenter','dragover'].forEach(ev=>drop?.addEventListener(ev,e=>{e.preventDefault();drop.classList.add('is-dragover')})); ['dragleave','drop'].forEach(ev=>drop?.addEventListener(ev,e=>{e.preventDefault();drop.classList.remove('is-dragover')})); drop?.addEventListener('drop',e=>handle(e.dataTransfer.files)); primary?.addEventListener('input',()=>setPreview(root,primary.value.trim()));
  }
  initManagers(); document.querySelectorAll('[data-admin-image-uploader]').forEach(init);
})();
