import {Component, inject} from '@angular/core';
import {Header} from './layout/header/header';
import {Footer} from './layout/footer/footer';
import {RouterOutlet} from '@angular/router';
import {Nav} from './layout/nav/nav';
import {AccountService} from './core/services/account.service';

@Component({
  selector: 'app-root',
  imports: [
    Header,
    Footer,
    RouterOutlet,
    Nav,
  ],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  accountService = inject(AccountService);

}
