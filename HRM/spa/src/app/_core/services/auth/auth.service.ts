import { Injectable } from '@angular/core';
import { environment } from '@env/environment';
import { JwtHelperService } from '@auth0/angular-jwt';
import { UserForLogged, UserLoginParam } from '@models/auth/auth';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { LocalStorageConstants } from '@constants/local-storage.constants';
import { KeyValuePair } from '@utilities/key-value-pair';
import { CacheService } from './../cache.service';
import { OperationResult } from '@utilities/operation-result';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  apiUrl = environment.apiUrl;
  jwtHelper = new JwtHelperService();

  constructor(
    private http: HttpClient,
    private router: Router,
    private cache: CacheService,
  ) { }

  login(param: UserLoginParam) {
    return this.http.post<OperationResult>(this.apiUrl + 'Auth/login', param);
  }

  logout = () => {
    const user = localStorage.getItem(LocalStorageConstants.USER)
    const token = localStorage.getItem(LocalStorageConstants.TOKEN)
    if (user)
      localStorage.removeItem(LocalStorageConstants.USER);
    if (token)
      localStorage.removeItem(LocalStorageConstants.TOKEN);
    this.cache.clearCache();
    sessionStorage.clear();
    this.router.navigate(['/login']);
  }

  loggedIn() {
    const token: string = localStorage.getItem(LocalStorageConstants.TOKEN);
    const user: UserForLogged = JSON.parse(localStorage.getItem(LocalStorageConstants.USER));
    return user && token
  }
  loggedInExpired() {
    const token: string = localStorage.getItem(LocalStorageConstants.TOKEN);
    return !token || (token && this.jwtHelper.isTokenExpired(token))
  }
  getListFactory() {
    return this.http.get<KeyValuePair[]>(this.apiUrl + 'Auth/GetListFactory');
  }
  getDirection() {
    return this.http.get<KeyValuePair[]>(this.apiUrl + 'Auth/GetDirection');
  }
  getProgram(direction: string) {
    return this.http.get<KeyValuePair[]>(this.apiUrl + 'Auth/GetProgram', { params: { direction } });
  }
  getListLangs() {
    return this.http.get<string[]>(this.apiUrl + 'Auth/GetListLangs');
  }
}
