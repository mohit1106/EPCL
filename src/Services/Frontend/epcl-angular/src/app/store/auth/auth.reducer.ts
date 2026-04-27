import { createReducer, on } from '@ngrx/store';
import { UserDto } from '../../core/services/auth-api.service';
import * as AuthActions from './auth.actions';

export interface AuthState {
  user: UserDto | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
  registerMessage: string | null;
}

export const initialState: AuthState = {
  user: null,
  token: null,
  isAuthenticated: false,
  isLoading: false,
  error: null,
  registerMessage: null,
};

export const authReducer = createReducer(
  initialState,

  // Login
  on(AuthActions.login, AuthActions.googleLogin, (state) => ({
    ...state,
    isLoading: true,
    error: null,
  })),

  on(AuthActions.loginSuccess, (state, { response }) => ({
    ...state,
    user: response.user,
    token: response.accessToken,
    isAuthenticated: true,
    isLoading: false,
    error: null,
  })),

  on(AuthActions.loginFailure, (state, { error }) => ({
    ...state,
    isLoading: false,
    error,
  })),

  // Register
  on(AuthActions.register, (state) => ({
    ...state,
    isLoading: true,
    error: null,
    registerMessage: null,
  })),

  on(AuthActions.registerSuccess, (state, { message }) => ({
    ...state,
    isLoading: false,
    registerMessage: message,
  })),

  on(AuthActions.registerFailure, (state, { error }) => ({
    ...state,
    isLoading: false,
    error,
  })),

  // Logout
  on(AuthActions.logoutComplete, () => ({
    ...initialState,
  })),

  // Session restore
  on(AuthActions.restoreSessionSuccess, (state, { user, token }) => ({
    ...state,
    user,
    token,
    isAuthenticated: true,
  })),

  on(AuthActions.restoreSessionFailure, () => ({
    ...initialState,
  })),

  // Load current user
  on(AuthActions.loadCurrentUserSuccess, (state, { user }) => ({
    ...state,
    user,
  }))
);
