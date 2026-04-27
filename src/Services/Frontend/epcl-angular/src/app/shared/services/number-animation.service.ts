import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class NumberAnimationService {
  animateNumber(
    start: number,
    end: number,
    duration: number,
    callback: (val: number) => void
  ): void {
    const startTime = performance.now();
    const animate = (currentTime: number) => {
      const elapsed = currentTime - startTime;
      const progress = Math.min(elapsed / duration, 1);
      const eased = 1 - Math.pow(1 - progress, 4); // easeOutQuart
      callback(Math.floor(start + (end - start) * eased));
      if (progress < 1) {
        requestAnimationFrame(animate);
      } else {
        callback(end);
      }
    };
    requestAnimationFrame(animate);
  }

  animateDecimal(
    start: number,
    end: number,
    duration: number,
    decimals: number,
    callback: (val: string) => void
  ): void {
    const startTime = performance.now();
    const animate = (currentTime: number) => {
      const elapsed = currentTime - startTime;
      const progress = Math.min(elapsed / duration, 1);
      const eased = 1 - Math.pow(1 - progress, 4);
      const value = start + (end - start) * eased;
      callback(value.toFixed(decimals));
      if (progress < 1) {
        requestAnimationFrame(animate);
      } else {
        callback(end.toFixed(decimals));
      }
    };
    requestAnimationFrame(animate);
  }
}
