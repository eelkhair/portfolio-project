"use client";

import { Swiper, SwiperSlide } from "swiper/react";
import type { Swiper as SwiperType } from "swiper";
import { Navigation, Pagination, Keyboard, Autoplay, A11y } from "swiper/modules";
import "swiper/css";
import "swiper/css/navigation";
import "swiper/css/pagination";
import { useEffect, useRef, useState } from "react";

function shuffle<T>(arr: T[]): T[] {
  const a = [...arr];
  for (let i = a.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [a[i], a[j]] = [a[j], a[i]];
  }
  return a;
}

type Testimonial = {
  quote: string;
  name: string;
  title: string;
  relationship: string;
  linkedInUrl: string;
};

export function TestimonialsCarousel({ testimonials }: { testimonials: Testimonial[] }) {
  const prevRef = useRef<HTMLButtonElement>(null);
  const nextRef = useRef<HTMLButtonElement>(null);
  const [items, setItems] = useState(testimonials);

  useEffect(() => {
    setItems(shuffle(testimonials));
  }, [testimonials]);

  const reducedMotion =
    typeof window !== "undefined" &&
    window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  return (
    <div className="testimonials-carousel">
    <div className="testimonials-carousel-wrapper">
      <button
        ref={prevRef}
        type="button"
        className="testimonials-nav-btn testimonials-prev"
        aria-label="Previous testimonial"
      >
        &#10094;
      </button>
      <Swiper
        className="testimonials-swiper"
        modules={[Navigation, Pagination, Keyboard, Autoplay, A11y]}
        grabCursor
        loop
        speed={500}
        spaceBetween={16}
        autoplay={
          reducedMotion
            ? false
            : { delay: 6000, disableOnInteraction: false, pauseOnMouseEnter: true }
        }
        pagination={{ el: ".testimonials-pagination", clickable: true }}
        keyboard={{ enabled: true }}
        a11y={{
          prevSlideMessage: "Previous testimonial",
          nextSlideMessage: "Next testimonial",
        }}
        onBeforeInit={(swiper: SwiperType) => {
          if (
            swiper.params.navigation &&
            typeof swiper.params.navigation !== "boolean"
          ) {
            swiper.params.navigation.prevEl = prevRef.current;
            swiper.params.navigation.nextEl = nextRef.current;
          }
        }}
        navigation={{ prevEl: prevRef.current, nextEl: nextRef.current }}
        breakpoints={{
          0: { slidesPerView: 1 },
          768: { slidesPerView: 2 },
        }}
      >
        {items.map((t) => (
          <SwiperSlide key={t.name}>
            <article className="card testimonial">
              <div className="testimonial-quote" aria-hidden="true">&ldquo;</div>
              <p className="testimonial-body">{t.quote}</p>
              <div className="testimonial-attribution">
                <div className="testimonial-name">{t.name}</div>
                <div className="testimonial-title">{t.title}</div>
                <div className="testimonial-meta">{t.relationship}</div>
              </div>
              {t.linkedInUrl && t.linkedInUrl !== "#" && (
                <a
                  href={t.linkedInUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="testimonial-link"
                >
                  Read on LinkedIn &rarr;
                </a>
              )}
            </article>
          </SwiperSlide>
        ))}
      </Swiper>
      <button
        ref={nextRef}
        type="button"
        className="testimonials-nav-btn testimonials-next"
        aria-label="Next testimonial"
      >
        &#10095;
      </button>
    </div>
    <div className="testimonials-pagination" aria-hidden="true" />
    </div>
  );
}
