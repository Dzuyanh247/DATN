(()=>{
 const modalEl=document.getElementById('pickModal'); if(!modalEl)return;
 const modal=new bootstrap.Modal(modalEl); let currentType='';
 const listEl=document.getElementById('productList'); const searchEl=document.getElementById('searchKeyword'); const sortEl=document.getElementById('sortPrice');
 async function loadProducts(){
  const q=new URLSearchParams({type:currentType,keyword:searchEl.value||'',sort:sortEl.value});
  const res=await fetch(`/buildpc/products?${q.toString()}`); const data=await res.json();
  listEl.innerHTML=data.map(x=>`<div class='d-flex border p-2 mb-2 align-items-center gap-2'><img src='${x.imageUrl}' style='width:56px;height:56px;object-fit:cover'/><div class='flex-grow-1'><div>${x.name}</div><div class='text-danger fw-bold'>${Number(x.price).toLocaleString('vi-VN')} đ</div><small>Tồn kho: ${x.stockQuantity}</small></div><button class='btn btn-sm btn-primary' data-pid='${x.id}'>Chọn</button></div>`).join('');
 }
 document.querySelectorAll('.btn-select').forEach(b=>b.addEventListener('click',()=>{currentType=b.dataset.type;loadProducts();modal.show();}));
 searchEl.addEventListener('input',loadProducts); sortEl.addEventListener('change',loadProducts);
 listEl.addEventListener('click',async e=>{const btn=e.target.closest('[data-pid]'); if(!btn)return; await fetch('/buildpc/select',{method:'POST',headers:{'Content-Type':'application/x-www-form-urlencoded'},body:`type=${encodeURIComponent(currentType)}&productId=${btn.dataset.pid}`}); location.reload();});
 document.querySelectorAll('.btn-remove').forEach(b=>b.addEventListener('click',async()=>{await fetch('/buildpc/remove',{method:'POST',headers:{'Content-Type':'application/x-www-form-urlencoded'},body:`type=${encodeURIComponent(b.dataset.type)}`});location.reload();}));
 document.getElementById('btnReset')?.addEventListener('click',async()=>{await fetch('/buildpc/reset',{method:'POST'});location.reload();});
 document.getElementById('btnCapture')?.addEventListener('click',async()=>{if(!window.html2canvas){alert('Chưa hỗ trợ tải ảnh trong môi trường này.');return;}const canvas=await html2canvas(document.getElementById('buildpcArea'));const a=document.createElement('a');a.href=canvas.toDataURL('image/png');a.download='buildpc.png';a.click();});
})();
