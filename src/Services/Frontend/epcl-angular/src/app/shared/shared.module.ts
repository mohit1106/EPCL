import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

import { ToastContainerComponent } from './components/toast-container/toast-container.component';
import { AiChatPanelComponent } from './components/ai-chat-panel/ai-chat-panel.component';
import { IconComponent } from './components/icon/icon.component';
import { MaxPipe } from './pipes/max.pipe';

@NgModule({
  declarations: [
    ToastContainerComponent,
    AiChatPanelComponent,
    IconComponent,
    MaxPipe,
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
  ],
  exports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    ToastContainerComponent,
    AiChatPanelComponent,
    IconComponent,
    MaxPipe,
  ],
})
export class SharedModule {}
