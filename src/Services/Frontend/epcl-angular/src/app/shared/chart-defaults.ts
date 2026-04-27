import { ChartOptions } from 'chart.js';

export const EPCL_CHART_DEFAULTS: ChartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      labels: { color: '#94A3B8', font: { family: 'Inter', size: 12 } },
    },
    tooltip: {
      backgroundColor: '#1E293B',
      borderColor: '#334155',
      borderWidth: 1,
      titleColor: '#F8FAFC',
      bodyColor: '#94A3B8',
      padding: 12,
      cornerRadius: 8,
      displayColors: true,
    },
  },
  scales: {
    x: {
      grid: { color: '#334155' },
      ticks: { color: '#64748B', font: { family: 'Inter', size: 11 } },
    },
    y: {
      grid: { color: '#334155' },
      ticks: { color: '#64748B', font: { family: 'Inter', size: 11 } },
    },
  },
};

export const CHART_COLORS = ['#2563EB', '#06B6D4', '#8B5CF6', '#10B981', '#F59E0B'];

export const BAR_OPTIONS: ChartOptions<'bar'> = {
  ...(EPCL_CHART_DEFAULTS as ChartOptions<'bar'>),
  elements: {
    bar: { borderRadius: 6, borderSkipped: false },
  },
};

export const LINE_OPTIONS: ChartOptions<'line'> = {
  ...(EPCL_CHART_DEFAULTS as ChartOptions<'line'>),
  elements: {
    line: { tension: 0.4 },
    point: { radius: 3, hoverRadius: 6 },
  },
};

export const DOUGHNUT_OPTIONS: ChartOptions<'doughnut'> = {
  responsive: true,
  maintainAspectRatio: false,
  cutout: '70%',
  plugins: {
    legend: {
      position: 'right',
      labels: { color: '#94A3B8', font: { family: 'Inter', size: 12 }, padding: 16 },
    },
    tooltip: {
      backgroundColor: '#1E293B',
      borderColor: '#334155',
      borderWidth: 1,
      titleColor: '#F8FAFC',
      bodyColor: '#94A3B8',
      padding: 12,
      cornerRadius: 8,
    },
  },
};
