import { CanActivateFn } from '@angular/router';
import {inject} from '@angular/core';
import {AccountService} from '../services/account.service';

export const roleGuard: CanActivateFn = (route) => {
  const accountService = inject(AccountService);
  const allowedGroups = route.data["groups"];
  const userGroups = (accountService.groups() ?? []).map(g => g.replace(/^\//, ''));
  return userGroups.some((group) => allowedGroups.includes(group));
};
