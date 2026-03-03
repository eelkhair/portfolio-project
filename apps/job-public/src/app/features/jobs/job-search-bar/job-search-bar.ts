import { Component, inject, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SearchStore } from '../../../core/stores/search.store';

@Component({
  selector: 'app-job-search-bar',
  imports: [FormsModule],
  templateUrl: './job-search-bar.html',
})
export class JobSearchBar {
  protected readonly searchStore = inject(SearchStore);
  readonly searched = output<void>();

  protected query = signal(this.searchStore.query());
  protected jobType = signal(this.searchStore.jobType());
  protected location = signal(this.searchStore.location());

  onSearch(): void {
    this.searchStore.search(this.query(), this.jobType(), this.location());
    this.searched.emit();
  }

  onClear(): void {
    this.query.set('');
    this.jobType.set('');
    this.location.set('');
    this.searchStore.clear();
  }
}
