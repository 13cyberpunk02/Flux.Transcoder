import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { TokenStore } from './token.store';

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const tokens = inject(TokenStore);
  const router = inject(Router);

  const token = tokens.token();
  if (token) {
    req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }

  return next(req).pipe(
    catchError((err: unknown) => {
      // токен истёк или отозван — на логин (кроме самого запроса логина)
      if (err instanceof HttpErrorResponse && err.status === 401 && !req.url.includes('/api/auth/login')) {
        tokens.clear();
        router.navigate(['/login']);
      }
      return throwError(() => err);
    }),
  );
};
