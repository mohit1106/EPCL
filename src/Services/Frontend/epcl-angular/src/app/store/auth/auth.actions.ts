import { createAction, props } from '@ngrx/store';
import { UserDto, LoginRequest, RegisterRequest, LoginResponse } from '../../core/services/auth-api.service';

// Login
export const login = createAction('[Auth] Login', props<{ request: LoginRequest }>());
export const loginSuccess = createAction('[Auth] Login Success', props<{ response: LoginResponse }>());
export const loginFailure = createAction('[Auth] Login Failure', props<{ error: string }>());

// Google Login
export const googleLogin = createAction('[Auth] Google Login', props<{ idToken: string }>());

// Register
export const register = createAction('[Auth] Register', props<{ request: RegisterRequest }>());
export const registerSuccess = createAction('[Auth] Register Success', props<{ message: string }>());
export const registerFailure = createAction('[Auth] Register Failure', props<{ error: string }>());

// Logout
export const logout = createAction('[Auth] Logout');
export const logoutComplete = createAction('[Auth] Logout Complete');

// Session restore
export const restoreSession = createAction('[Auth] Restore Session');
export const restoreSessionSuccess = createAction('[Auth] Restore Session Success', props<{ user: UserDto; token: string }>());
export const restoreSessionFailure = createAction('[Auth] Restore Session Failure');

// User profile
export const loadCurrentUser = createAction('[Auth] Load Current User');
export const loadCurrentUserSuccess = createAction('[Auth] Load Current User Success', props<{ user: UserDto }>());
