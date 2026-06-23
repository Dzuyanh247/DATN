(function(){
const root=document.getElementById('voucherForm');if(!root)return;
const code=document.getElementById('Code'),type=document.getElementById('DiscountType'),value=document.getElementById('DiscountValue'),max=document.getElementById('MaxDiscountAmount'),min=document.getElementById('MinimumOrderAmount'),qty=document.getElementById('Quantity'),unlimited=document.getElementById('UnlimitedQuantity'),help=document.getElementById('discountValueHelp'),maxGroup=document.getElementById('maxDiscountGroup'),preview=document.getElementById('voucherPreview'),start=document.getElementById('StartDate'),end=document.getElementById('EndDate');
const digits=v=>(v||'').toString().replace(/[^0-9]/g,'');
const number=v=>Number(digits(v)||0);
const money=v=>number(v).toLocaleString('vi-VN')+' VNĐ';
const setPreview=(key,text)=>{const el=preview?.querySelector(`[data-preview="${key}"]`);if(el)el.textContent=text;};
const dateText=v=>{if(!v)return'';const d=new Date(v);return Number.isNaN(d.getTime())?'':d.toLocaleString('vi-VN',{day:'2-digit',month:'2-digit',year:'numeric',hour:'2-digit',minute:'2-digit'});};
function formatMoney(el){if(!el)return;const d=digits(el.value);el.value=d?Number(d).toLocaleString('vi-VN'):'';}
function sync(){if(code)code.value=code.value.toUpperCase().replace(/\s+/g,'');const isPercent=type.value==='Percent';help.textContent=isPercent?'Ví dụ: 10 cho voucher giảm 10%.':'Ví dụ: 100000 cho voucher giảm 100.000 VNĐ.';maxGroup.classList.toggle('d-none',!isPercent);if(max){max.disabled=!isPercent;if(!isPercent)max.value='';}if(qty&&unlimited){qty.disabled=unlimited.checked;if(unlimited.checked)qty.value='';}
const c=code.value||'MÃVOUCHER';const discount=number(value);const minText=money(min.value||'0');const maxText=number(max?.value)>0?`Tối đa ${money(max.value)}`:'Không giới hạn';const qtyText=unlimited?.checked?'Không giới hạn':(qty?.value?Number(qty.value).toLocaleString('vi-VN'):'Theo số lượng nhập');const startText=dateText(start?.value);const endText=dateText(end?.value);
setPreview('code',c);setPreview('type',isPercent?'Giảm theo phần trăm':'Giảm số tiền cố định');setPreview('value',isPercent?`${discount||0}% · ${maxText}`:money(value));setPreview('minimum',minText);setPreview('quantity',qtyText);setPreview('time',startText&&endText?`${startText} → ${endText}`:'Chọn ngày bắt đầu và kết thúc');}
[ value,max,min ].forEach(el=>{el&&el.addEventListener('input',()=>formatMoney(el));});
root.addEventListener('input',sync);root.addEventListener('change',sync);root.closest('form')?.addEventListener('submit',()=>{[value,max,min].forEach(el=>{if(el)el.value=digits(el.value);});if(qty&&unlimited?.checked){qty.disabled=false;qty.value='2147483647';}});
[value,max,min].forEach(formatMoney);sync();
})();
