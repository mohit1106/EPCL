import { createFeatureSelector, createSelector } from '@ngrx/store';
import { AuthState } from './auth.reducer';

export const selectAuthState = createFeatureSelector<AuthState>('auth');

export const selectIsAuthenticated = createSelector(
  selectAuthState,
  (state) => state.isAuthenticated
);

export const selectUser = createSelector(
  selectAuthState,
  (state) => state.user
);

export const selectUserRole = createSelector(
  selectAuthState,
  (state) => state.user?.role || null
);

export const selectToken = createSelector(
  selectAuthState,
  (state) => state.token
);

export const selectAuthLoading = createSelector(
  selectAuthState,
  (state) => state.isLoading
);

export const selectAuthError = createSelector(
  selectAuthState,
  (state) => state.error
);

export const selectRegisterMessage = createSelector(
  selectAuthState,
  (state) => state.registerMessage
);

export const selectUserFullName = createSelector(
  selectUser,
  (user) => user?.fullName || ''
);

export const selectUserEmail = createSelector(
  selectUser,
  (user) => user?.email || ''
);

export const selectUserProfilePicture = createSelector(
  selectUser,
  (user) => user?.profilePictureUrl || null
);
