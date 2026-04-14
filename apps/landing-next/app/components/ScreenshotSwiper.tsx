"use client";

import { Swiper, SwiperSlide } from "swiper/react";
import { Navigation, Pagination, Keyboard, Autoplay, EffectCoverflow, Parallax } from "swiper/modules";
import "swiper/css";
import "swiper/css/navigation";
import "swiper/css/pagination";
import "swiper/css/effect-coverflow";
import { useEffect, useRef, useState, useCallback } from "react";

const screenshots = [
  { src: "/images/aspire-resources.png", title: "Aspire Dashboard", desc: "Orchestrating 36 resources with dependency ordering and health checks" },
  { src: "/images/jaeger-trace.png", title: "Distributed Trace", desc: "End-to-end trace across services in Jaeger" },
  { src: "/images/aspire-graph.png", title: "Topology Graph", desc: "Aspire service dependency topology" },
  { src: "/images/saga-trace.png", title: "Saga Orchestration", desc: "Async saga and pub/sub visibility in traces" },
  { src: "/images/grafana-traceid.png", title: "Grafana Debugging", desc: '"Find by TraceId" debugging workflow' },
  { src: "/images/healthchecks.png", title: "Health Checks", desc: "Centralized dependency monitoring dashboard" },
  { src: "/images/dapr-dashboard.png", title: "Dapr Dashboard", desc: "Service mesh applications overview" },
];

export function ScreenshotSwiper() {
  const sectionRef = useRef<HTMLDivElement>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [currentModal, setCurrentModal] = useState(0);

  useEffect(() => {
    const el = sectionRef.current;
    if (!el) return;
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add("visible");
            observer.unobserve(entry.target);
          }
        });
      },
      { threshold: 0.15 }
    );
    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  const openModal = useCallback((index: number) => {
    setCurrentModal(index);
    setModalOpen(true);
    document.body.style.overflow = "hidden";
  }, []);

  const closeModal = useCallback(() => {
    setModalOpen(false);
    document.body.style.overflow = "";
  }, []);

  const nextImage = useCallback(() => {
    setCurrentModal((prev) => (prev + 1) % screenshots.length);
  }, []);

  const prevImage = useCallback(() => {
    setCurrentModal((prev) => (prev - 1 + screenshots.length) % screenshots.length);
  }, []);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (!modalOpen) return;
      if (e.key === "Escape") closeModal();
      if (e.key === "ArrowRight") nextImage();
      if (e.key === "ArrowLeft") prevImage();
    };
    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [modalOpen, closeModal, nextImage, prevImage]);

  const reducedMotion = typeof window !== "undefined" && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  return (
    <>
      <div id="screenshots" ref={sectionRef}>
        <Swiper
          className="screenshot-swiper"
          modules={[Navigation, Pagination, Keyboard, Autoplay, EffectCoverflow, Parallax]}
          effect="coverflow"
          grabCursor
          centeredSlides
          rewind
          speed={800}
          autoplay={reducedMotion ? false : { delay: 5000, disableOnInteraction: false, pauseOnMouseEnter: true }}
          coverflowEffect={{ rotate: 3, stretch: 0, depth: 200, modifier: 1, slideShadows: true }}
          parallax
          pagination={{ clickable: true }}
          navigation
          keyboard={{ enabled: true }}
          breakpoints={{
            0: { slidesPerView: 1.15 },
            480: { slidesPerView: 1.4 },
            768: { slidesPerView: 1.8 },
            1024: { slidesPerView: 2.2 },
          }}
        >
          {screenshots.map((s, i) => (
            <SwiperSlide key={i}>
              <div className="screenshot-item" onClick={() => openModal(i)} data-index={i}>
                <div className="screenshot-img-wrapper">
                  {/* eslint-disable-next-line @next/next/no-img-element */}
                  <img src={s.src} alt={`${s.title} \u2014 ${s.desc}`} loading="lazy" />
                </div>
                <div className="screenshot-caption" data-swiper-parallax="-100">
                  <span className="caption-title">{s.title}</span>
                  <span className="caption-desc">{s.desc}</span>
                </div>
              </div>
            </SwiperSlide>
          ))}
        </Swiper>
      </div>

      {/* Modal */}
      <div
        className={`screenshot-modal${modalOpen ? " active" : ""}`}
        role="dialog"
        aria-modal="true"
        aria-label="Screenshot viewer"
        onClick={(e) => { if (e.target === e.currentTarget) closeModal(); }}
      >
        <button className="modal-close" aria-label="Close viewer" onClick={closeModal}>&times;</button>
        <div className="modal-counter" aria-live="polite">{currentModal + 1} / {screenshots.length}</div>
        <button className="modal-nav modal-prev" aria-label="Previous screenshot" onClick={(e) => { e.stopPropagation(); prevImage(); }}>&#10094;</button>
        <button className="modal-nav modal-next" aria-label="Next screenshot" onClick={(e) => { e.stopPropagation(); nextImage(); }}>&#10095;</button>
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img
          src={screenshots[currentModal].src}
          alt={`${screenshots[currentModal].title} \u2014 ${screenshots[currentModal].desc}`}
          onClick={(e) => { e.stopPropagation(); (e.target as HTMLImageElement).classList.toggle("zoomed"); }}
        />
        <div className="modal-caption">{screenshots[currentModal].title} &mdash; {screenshots[currentModal].desc}</div>
      </div>
    </>
  );
}
