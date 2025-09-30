export interface UserLoginParam {
  username: string;
  password: string;
  factory: string;
  lang: string;
}
export interface UserForLogged {
  id: string;
  factory: string;
  account: string;
  name: string;
}
export interface ResultResponse {
  user: UserForLogged;
  token: string;
}
