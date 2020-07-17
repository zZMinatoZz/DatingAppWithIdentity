import { Component, OnInit, Input } from '@angular/core';
import { Message } from 'src/app/_models/message';
import { UserService } from 'src/app/_services/user.service';
import { AuthService } from 'src/app/_services/auth.service';
import { AlertifyService } from 'src/app/_services/alertify.service';
import { tap } from 'rxjs/operators';

@Component({
  selector: 'app-member-messages',
  templateUrl: './member-messages.component.html',
  styleUrls: ['./member-messages.component.css']
})
export class MemberMessagesComponent implements OnInit {
  @Input() recipientId: number;
  messages: Message[];
  newMessage: any = {};

  constructor(private userService: UserService,
    private authService: AuthService, private alertify: AlertifyService) { }

  ngOnInit() {
    this.loadMessages();
  }

  loadMessages() {
    // get current user id
    const currentUserId = +this.authService.decodedToken.nameid;
    // su dung 'tap' de check event khi cham vao element
    // trc khi subscribe de get dc list messages trong conversation, thuc hien goi 'pipe'
    // xy ly neu user 'tap' vao bat ki message nao trong list, duyet toan bo message trong list, neu messenger nao
    // isRead = false va id recipient cua message do chinh la current user thi thuc hien mark as read
    this.userService.getMessageThread(this.authService.decodedToken.nameid, this.recipientId)
      .pipe(
        tap(messages => {
          for (let i = 0; i < messages.length; i++) {
            if (messages[i].isRead === false && messages[i].recipientId === currentUserId) {
              this.userService.markAsRead(currentUserId, messages[i].id);
            }
          }
        })
      )
      .subscribe(messages => {
        this.messages = messages;
      }, error => {
        this.alertify.error(error);
      });
  }

  sendMessage() {
    this.newMessage.recipientId = this.recipientId;
    this.userService.sendMessage(this.authService.decodedToken.nameid, this.newMessage)
    .subscribe((message: Message) => {
      // unshift: add 1 element to array at start position
      this.messages.unshift(message);
      // reset form
      this.newMessage.content = '';
    }, error => {
      this.alertify.error(error);
    });
  }

}
