import {Component, computed, inject, OnInit, signal} from '@angular/core';
import {DashboardService} from '../../core/services/dashboard.service';
import {DashboardResponse, RecentJobItem} from '../../core/types/Dtos/DashboardResponse';
import {UIChart} from 'primeng/chart';
import {Skeleton} from 'primeng/skeleton';
import {Card} from 'primeng/card';
import {Carousel} from 'primeng/carousel';
import {DatePipe} from '@angular/common';

interface CarouselCard {
  title: string;
  kind: 'chart' | 'recentJobs';
  chartType?: 'bar' | 'doughnut';
  data?: any;
  options?: any;
  recentJobs?: RecentJobItem[];
}

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
  imports: [UIChart, Skeleton, Card, Carousel, DatePipe]
})
export class Dashboard implements OnInit {
  private dashboardService = inject(DashboardService);

  dashboard = signal<DashboardResponse | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  private barColors = ['#42A5F5', '#66BB6A', '#FFA726', '#AB47BC', '#EF5350', '#26C6DA', '#EC407A', '#8D6E63'];

  carouselCards = computed<CarouselCard[]>(() => {
    const d = this.dashboard();
    if (!d) return [];

    const cards: CarouselCard[] = [];

    if (d.jobsByType?.length) {
      cards.push({
        title: 'Jobs by Type',
        kind: 'chart',
        chartType: 'doughnut',
        data: {
          labels: d.jobsByType.map(x => x.label),
          datasets: [{
            data: d.jobsByType.map(x => x.count),
            backgroundColor: ['#42A5F5', '#66BB6A', '#FFA726', '#AB47BC'],
            hoverBackgroundColor: ['#64B5F6', '#81C784', '#FFB74D', '#CE93D8']
          }]
        },
        options: this.doughnutOptions
      });
    }

    if (d.recentJobs?.length) {
      cards.push({
        title: 'Recent Jobs',
        kind: 'recentJobs',
        recentJobs: d.recentJobs
      });
    }

    if (d.topCompanies?.length) {
      cards.push({
        title: 'Top Companies',
        kind: 'chart',
        chartType: 'bar',
        data: {
          labels: d.topCompanies.map(c => c.label),
          datasets: [{
            label: 'Jobs',
            data: d.topCompanies.map(c => c.count),
            backgroundColor: this.barColors.slice(0, d.topCompanies.length),
            borderRadius: 4,
            borderSkipped: false
          }]
        },
        options: this.horizontalBarOptions
      });
    }

    if (d.jobsByLocation?.length) {
      cards.push({
        title: 'Jobs by Location',
        kind: 'chart',
        chartType: 'bar',
        data: {
          labels: d.jobsByLocation.map(l => l.label),
          datasets: [{
            label: 'Jobs',
            data: d.jobsByLocation.map(l => l.count),
            backgroundColor: this.barColors.slice(0, d.jobsByLocation.length),
            borderRadius: 4,
            borderSkipped: false
          }]
        },
        options: this.horizontalBarOptions
      });
    }

    return cards;
  });

  doughnutOptions = {
    maintainAspectRatio: false,
    layout: {
      padding: {bottom: 10}
    },
    plugins: {
      legend: {
        position: 'bottom' as const,
        labels: {
          color: '#adb5bd',
          padding: 20
        }
      }
    }
  };

  horizontalBarOptions = {
    indexAxis: 'y' as const,
    maintainAspectRatio: false,
    animation: false as const,
    plugins: {
      legend: {display: false}
    },
    scales: {
      x: {
        ticks: {color: '#adb5bd'},
        grid: {color: 'rgba(255,255,255,0.05)'}
      },
      y: {
        ticks: {color: '#adb5bd', padding: 8},
        grid: {display: false}
      }
    },
    barPercentage: 0.6,
    categoryPercentage: 1
  };

  responsiveCarouselOptions = [
    {breakpoint: '768px', numVisible: 1, numScroll: 1}
  ];

  ngOnInit() {
    this.loadDashboard();
  }

  loadDashboard() {
    this.loading.set(true);
    this.error.set(null);

    this.dashboardService.getDashboard().subscribe({
      next: (res) => {
        if (res.success) {
          this.dashboard.set(res.data ?? null);
        } else {
          this.error.set('Failed to load dashboard data');
        }
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to connect to the server');
        this.loading.set(false);
      }
    });
  }
}
