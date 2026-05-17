import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // Get the token from local storage (or another secure location)
  const authToken = localStorage.getItem('auth_token');

  // Clone the request and add the authorization header if token exists
  const authReq = authToken 
    ? req.clone({
        setHeaders: {
          Authorization: `Bearer ${authToken}`
        }
      })
    : req;

  // Pass on the cloned request
  return next(authReq);
};
