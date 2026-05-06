(() => {
  const urlInput = document.querySelector('[data-banner-url]');
  const preview = document.querySelector('[data-banner-preview]');
  const position = document.querySelector('[data-banner-position]');
  const mainHint = document.querySelector('[data-main-size-hint]');
  const subHint = document.querySelector('[data-sub-size-hint]');
  const placeholder = '/images/placeholders/banner-placeholder.png';

  const renderPreview = () => {
    if (!preview) return;
    const url = urlInput?.value?.trim();
    preview.style.backgroundImage = `url('${url || placeholder}')`;
  };

  urlInput?.addEventListener('input', renderPreview);
  renderPreview();

  const syncHint = () => {
    const isSub = position?.value === 'SubBanner';
    mainHint?.classList.toggle('d-none', isSub);
    subHint?.classList.toggle('d-none', !isSub);
  };
  position?.addEventListener('change', syncHint);
  syncHint();
})();
