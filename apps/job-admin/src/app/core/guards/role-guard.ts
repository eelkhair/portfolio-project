import { CanActivateFn } from '@angular/router';
import {inject} from '@angular/core';
import {AccountService} from '../services/account.service';

export const roleGuard: CanActivateFn = (route) => {
  const accountService = inject(AccountService);
  const allowedRoles = route.data["roles"];
  const userRoles = accountService.roles() ?? [];
  return userRoles.some((role) => allowedRoles.includes(role));
};
