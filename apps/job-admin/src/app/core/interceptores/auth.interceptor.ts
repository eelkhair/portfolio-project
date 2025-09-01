import { HttpInterceptorFn } from '@angular/common/http';
import {inject} from '@angular/core';
import {from, mergeMap} from 'rxjs';
import {AuthService} from '@auth0/auth0-angular';


export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  return from(auth.getAccessTokenSilently()).pipe(
    mergeMap(token => {
      const authReq = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
      return next(authReq);
    })
  );
};
