(() => {
  const fileInput = document.querySelector('[data-banner-file]');
  const preview = document.querySelector('[data-banner-preview]');
  const position = document.querySelector('[data-banner-position]');
  const mainHint = document.querySelector('[data-main-size-hint]');
  const subHint = document.querySelector('[data-sub-size-hint]');
  const setPlaceholder = () => preview && (preview.style.backgroundImage = 'linear-gradient(120deg,#edf2f7,#dbe4ee)');

  if (preview && !preview.dataset.currentImage && !preview.style.backgroundImage) setPlaceholder();

  fileInput?.addEventListener('change', (e) => {
    const file = e.target.files?.[0];
    if (!file || !preview) return setPlaceholder();
    const reader = new FileReader();
    reader.onload = (ev) => { preview.style.backgroundImage = `url('${ev.target?.result}')`; };
    reader.onerror = setPlaceholder;
    reader.readAsDataURL(file);
  });

  const syncHint = () => {
    const isSub = position?.value === 'SubBanner';
    mainHint?.classList.toggle('d-none', isSub);
    subHint?.classList.toggle('d-none', !isSub);
  };

  position?.addEventListener('change', syncHint);
  syncHint();
})();
