import { Component, OnInit, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import { Subject, takeUntil, finalize } from 'rxjs';
import { AiApiService, ChatResponse, ConversationMessage } from '../../../core/services/ai-api.service';

interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  tableData?: Record<string, unknown>[] | null;
  columnNames?: string[] | null;
  hasChartData?: boolean;
  chartType?: string | null;
  rowsReturned?: number;
  timestamp: Date;
  isLoading?: boolean;
}

@Component({
  selector: 'app-ai-chat-panel',
  templateUrl: './ai-chat-panel.component.html',
  styleUrls: ['./ai-chat-panel.component.scss'],
})
export class AiChatPanelComponent implements OnInit, OnDestroy {
  @ViewChild('chatBody') chatBody!: ElementRef<HTMLDivElement>;
  @ViewChild('messageInput') messageInput!: ElementRef<HTMLTextAreaElement>;

  private destroy$ = new Subject<void>();

  isOpen = false;
  messages: ChatMessage[] = [];
  suggestions: string[] = [];
  inputText = '';
  isLoading = false;
  sessionId: string | null = null;
  showTable: Record<number, boolean> = {};

  constructor(private aiApi: AiApiService) {}

  ngOnInit(): void {
    this.loadSuggestions();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  toggle(): void {
    this.isOpen = !this.isOpen;
    if (this.isOpen && this.messages.length === 0) {
      this.loadSuggestions();
    }
    if (this.isOpen) {
      setTimeout(() => this.messageInput?.nativeElement?.focus(), 300);
    }
  }

  close(): void {
    this.isOpen = false;
  }

  loadSuggestions(): void {
    this.aiApi.getSuggestions().pipe(takeUntil(this.destroy$)).subscribe({
      next: (res) => this.suggestions = res.questions,
    });
  }

  useSuggestion(question: string): void {
    this.inputText = question;
    this.sendMessage();
  }

  sendMessage(): void {
    const text = this.inputText.trim();
    if (!text || this.isLoading) return;

    // Add user message
    this.messages.push({
      role: 'user',
      content: text,
      timestamp: new Date(),
    });

    // Add loading placeholder
    const loadingIdx = this.messages.length;
    this.messages.push({
      role: 'assistant',
      content: '',
      timestamp: new Date(),
      isLoading: true,
    });

    this.inputText = '';
    this.isLoading = true;
    this.scrollToBottom();

    this.aiApi.chat({ message: text, sessionId: this.sessionId ?? undefined })
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.isLoading = false),
      )
      .subscribe({
        next: (res: ChatResponse) => {
          this.sessionId = res.sessionId;
          // Replace loading placeholder with real response
          this.messages[loadingIdx] = {
            role: 'assistant',
            content: res.answer,
            tableData: res.tableData,
            columnNames: res.columnNames,
            hasChartData: res.hasChartData,
            chartType: res.chartType,
            rowsReturned: res.rowsReturned,
            timestamp: new Date(),
          };
          this.scrollToBottom();
        },
        error: () => {
          this.messages[loadingIdx] = {
            role: 'assistant',
            content: 'Sorry, I encountered an error. Please try again.',
            timestamp: new Date(),
          };
          this.scrollToBottom();
        },
      });
  }

  clearHistory(): void {
    this.aiApi.clearHistory(this.sessionId ?? undefined).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.messages = [];
        this.sessionId = null;
      },
    });
  }

  toggleTable(index: number): void {
    this.showTable[index] = !this.showTable[index];
  }

  copyResponse(content: string): void {
    navigator.clipboard.writeText(content);
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  trackByIndex(index: number): number {
    return index;
  }

  getTableKeys(row: Record<string, unknown>): string[] {
    return Object.keys(row);
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      if (this.chatBody) {
        this.chatBody.nativeElement.scrollTop = this.chatBody.nativeElement.scrollHeight;
      }
    }, 100);
  }
}
