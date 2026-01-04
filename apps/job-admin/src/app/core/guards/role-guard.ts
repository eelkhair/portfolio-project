import { CanActivateFn } from '@angular/router';
import {inject} from '@angular/core';
import {AccountService} from '../services/account.service';

export const roleGuard: CanActivateFn = (route) => {
  const accountService = inject(AccountService);
  const user = accountService.user();
  const allowedRoles = route.data["roles"];

  if (user) {
    const userRoles = user["https://eelkhair.net/roles"] as string[] ?? [];
    return userRoles.some((role) => { return allowedRoles.includes(role); });
  }
  return false;
};
