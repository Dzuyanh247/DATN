(()=>{
 const modalEl=document.getElementById('pickModal'); if(!modalEl)return;
 const modal=new bootstrap.Modal(modalEl); let currentType='';
 const listEl=document.getElementById('productList'); const searchEl=document.getElementById('searchKeyword'); const sortEl=document.getElementById('sortPrice');
 async function loadProducts(){
  const q=new URLSearchParams({type:currentType,keyword:searchEl.value||'',sort:sortEl.value});
  const res=await fetch(`/buildpc/products?${q.toString()}`);
  let data;
  try{data=await res.json();}
  catch(_){
   listEl.innerHTML="<div class='alert alert-warning mb-0'>Không tìm thấy sản phẩm phù hợp</div>";
   return;
  }
  console.log(data);
  const products=Array.isArray(data)?data:(Array.isArray(data?.products)?data.products:[]);
  if(products.length===0){
   const message=(data&&typeof data==='object'&&data.message)?data.message:'Không tìm thấy sản phẩm phù hợp';
   listEl.innerHTML=`<div class='alert alert-warning mb-0'>${message}</div>`;
   return;
  }
  listEl.innerHTML=products.map(product=>`<div class='d-flex border p-2 mb-2 align-items-center gap-2'><img src='${product.imageUrl||''}' style='width:56px;height:56px;object-fit:cover'/><div class='flex-grow-1'><div>${product.name||''}</div><div class='text-danger fw-bold'>${Number(product.price||0).toLocaleString('vi-VN')} đ</div><small>Danh mục: ${product.categoryName||'N/A'}</small><br/><small>Tồn kho: ${product.stockQuantity??0}</small><br/><small>Bảo hành: ${product.warranty||'N/A'}</small></div><button class='btn btn-sm btn-primary' data-pid='${product.id}'>Chọn</button></div>`).join('');
 }
 document.querySelectorAll('.btn-select').forEach(b=>b.addEventListener('click',()=>{currentType=b.dataset.type;loadProducts();modal.show();}));
 searchEl.addEventListener('input',loadProducts); sortEl.addEventListener('change',loadProducts);
 listEl.addEventListener('click',async e=>{const btn=e.target.closest('[data-pid]'); if(!btn)return; await fetch('/buildpc/select',{method:'POST',headers:{'Content-Type':'application/x-www-form-urlencoded'},body:`type=${encodeURIComponent(currentType)}&productId=${btn.dataset.pid}`}); location.reload();});
 document.querySelectorAll('.btn-remove').forEach(b=>b.addEventListener('click',async()=>{await fetch('/buildpc/remove',{method:'POST',headers:{'Content-Type':'application/x-www-form-urlencoded'},body:`type=${encodeURIComponent(b.dataset.type)}`});location.reload();}));
 document.getElementById('btnReset')?.addEventListener('click',async()=>{await fetch('/buildpc/reset',{method:'POST'});location.reload();});
 document.getElementById('btnCapture')?.addEventListener('click',async()=>{if(!window.html2canvas){window.kkToast?.warning('Chưa hỗ trợ tải ảnh trong môi trường này.');return;}const canvas=await html2canvas(document.getElementById('buildpcArea'));const a=document.createElement('a');a.href=canvas.toDataURL('image/png');a.download='buildpc.png';a.click();});
})();
