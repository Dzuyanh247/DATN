document.addEventListener('DOMContentLoaded', () => {
  document.querySelectorAll('[data-home-carousel]').forEach((carousel) => {
    const slides = Array.from(carousel.querySelectorAll('[data-carousel-slide]'));
    const dots = Array.from(carousel.querySelectorAll('[data-carousel-dot]'));
    const previousButton = carousel.querySelector('[data-carousel-prev]');
    const nextButton = carousel.querySelector('[data-carousel-next]');

    if (slides.length < 2) return;

    const autoplayDelay = 4500;
    let activeIndex = 0;
    let autoplayTimer;
    let isPointerOver = false;
    let hasFocus = false;

    const showSlide = (nextIndex) => {
      activeIndex = (nextIndex + slides.length) % slides.length;

      slides.forEach((slide, index) => {
        const isActive = index === activeIndex;
        slide.classList.toggle('is-active', isActive);
        slide.setAttribute('aria-hidden', String(!isActive));
        slide.querySelector('a')?.setAttribute('tabindex', isActive ? '0' : '-1');
      });

      dots.forEach((dot, index) => {
        const isActive = index === activeIndex;
        dot.classList.toggle('is-active', isActive);
        dot.setAttribute('aria-current', String(isActive));
      });
    };

    const stopAutoplay = () => window.clearInterval(autoplayTimer);
    const startAutoplay = () => {
      stopAutoplay();
      if (!isPointerOver && !hasFocus && !document.hidden) {
        autoplayTimer = window.setInterval(() => showSlide(activeIndex + 1), autoplayDelay);
      }
    };

    previousButton?.addEventListener('click', () => {
      showSlide(activeIndex - 1);
      startAutoplay();
    });

    nextButton?.addEventListener('click', () => {
      showSlide(activeIndex + 1);
      startAutoplay();
    });

    dots.forEach((dot) => {
      dot.addEventListener('click', () => {
        showSlide(Number(dot.dataset.carouselDot));
        startAutoplay();
      });
    });

    carousel.addEventListener('mouseenter', () => {
      isPointerOver = true;
      stopAutoplay();
    });

    carousel.addEventListener('mouseleave', () => {
      isPointerOver = false;
      startAutoplay();
    });

    carousel.addEventListener('focusin', () => {
      hasFocus = true;
      stopAutoplay();
    });

    carousel.addEventListener('focusout', (event) => {
      if (!carousel.contains(event.relatedTarget)) {
        hasFocus = false;
        startAutoplay();
      }
    });

    document.addEventListener('visibilitychange', startAutoplay);
    startAutoplay();
  });
});
