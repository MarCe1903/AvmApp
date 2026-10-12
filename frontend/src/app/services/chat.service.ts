import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';

export interface ChatSessionDto {
  id: number;
  userId: number;
}

export interface ChatSessionListItem {
  id: number;
  userId: number;
  fullName: string;
  email: string;
  lastMessageAt: string;
}

export interface ChatMessageDto {
  id: number;
  senderId: number;
  senderRole: string;
  text: string;
  sentAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  constructor(private http: HttpClient) {}

  getSessions(): Observable<ChatSessionListItem[]> {
    return this.http.get<ChatSessionListItem[]>(buildApiUrl('chats'));
  }

  startChat(): Observable<ChatSessionDto> {
    return this.http.post<ChatSessionDto>(buildApiUrl('chats/start'), {});
  }

  getMessages(sessionId: number): Observable<ChatMessageDto[]> {
    return this.http.get<ChatMessageDto[]>(buildApiUrl(`chats/${sessionId}/messages`));
  }

  sendMessage(sessionId: number, text: string): Observable<void> {
    return this.http.post<void>(buildApiUrl(`chats/${sessionId}/messages`), { text });
  }

  closeChat(sessionId: number): Observable<void> {
    return this.http.post<void>(buildApiUrl(`chats/${sessionId}/close`), {});
  }
}
