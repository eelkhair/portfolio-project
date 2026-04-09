export interface LabelCountItem {
  label: string;
  count: number;
}

export interface RecentJobItem {
  title: string;
  companyName: string;
  createdAt: string;
}

export interface DashboardResponse {
  jobCount: number;
  companyCount: number;
  draftCount: number;
  jobsByType: LabelCountItem[];
  jobsByLocation: LabelCountItem[];
  topCompanies: LabelCountItem[];
  recentJobs: RecentJobItem[];
}
