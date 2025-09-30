import { Injectable, inject } from '@angular/core';
import { AuthService } from '@services/auth/auth.service';
import { CanActivateFn, Router } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard {
  constructor(private authService: AuthService, private router: Router) { }
  canActivate(): boolean {
    if (this.authService.loggedIn() && !this.authService.loggedInExpired())
      return true;
    this.router.navigate(["/login"]);
    return false;
  }
}
export const authGuard: CanActivateFn = () => {
  return inject(AuthGuard).canActivate();
};
