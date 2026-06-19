(function(){
const root=document.getElementById('voucherForm');if(!root)return;
const code=document.getElementById('Code'),type=document.getElementById('DiscountType'),value=document.getElementById('DiscountValue'),max=document.getElementById('MaxDiscountAmount'),min=document.getElementById('MinimumOrderAmount'),qty=document.getElementById('Quantity'),unlimited=document.getElementById('UnlimitedQuantity'),help=document.getElementById('discountValueHelp'),maxGroup=document.getElementById('maxDiscountGroup'),preview=document.getElementById('voucherPreview');
const digits=v=>(v||'').toString().replace(/[^0-9]/g,'');
const number=v=>Number(digits(v)||0);
const money=v=>number(v).toLocaleString('vi-VN')+' VNĐ';
function formatMoney(el){if(!el)return;const d=digits(el.value);el.value=d?Number(d).toLocaleString('vi-VN'):'';}
function sync(){if(code)code.value=code.value.toUpperCase().replace(/\s+/g,'');const isPercent=type.value==='Percent';help.textContent=isPercent?'Ví dụ: 10 cho voucher giảm 10%.':'Ví dụ: 100000 cho voucher giảm 100.000 VNĐ.';maxGroup.classList.toggle('d-none',!isPercent);if(max){max.disabled=!isPercent;if(!isPercent)max.value='';}if(qty&&unlimited){qty.disabled=unlimited.checked;if(unlimited.checked)qty.value='';}
const c=code.value||'MÃVOUCHER';const discount=number(value);const minText=money(min.value||'0');let text;if(isPercent){text=`${c} - Giảm ${discount||0}%`;if(number(max.value)>0)text+=`, tối đa ${money(max.value)}`;text+=` cho đơn từ ${minText}`;}else{text=`${c} - Giảm ${money(value)} cho đơn từ ${minText}`;}preview.textContent=text;}
[ value,max,min ].forEach(el=>{el&&el.addEventListener('input',()=>formatMoney(el));});
root.addEventListener('input',sync);root.addEventListener('change',sync);root.closest('form')?.addEventListener('submit',()=>{[value,max,min].forEach(el=>{if(el)el.value=digits(el.value);});if(qty&&unlimited?.checked){qty.disabled=false;qty.value='2147483647';}});
[value,max,min].forEach(formatMoney);sync();
})();
