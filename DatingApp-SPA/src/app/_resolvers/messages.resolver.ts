import { Injectable } from '@angular/core';
import { Resolve, Router, ActivatedRouteSnapshot } from '@angular/router';
import { UserService } from '../_services/user.service';
import { AlertifyService } from '../_services/alertify.service';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Message } from '../_models/message';
import { AuthService } from '../_services/auth.service';

@Injectable()
export class MessagesResolver implements Resolve<Message[]> {
    pageNumber = 1;
    pageSize = 5;
    messageContainer = 'Unread';

    constructor (private userService: UserService, private authService: AuthService,
        private route: Router, private alertify: AlertifyService) {}

    resolve(route: ActivatedRouteSnapshot): Observable<Message[]> {
        return this.userService.getMessages(this.authService.decodedToken.nameid,
             this.pageNumber, this.pageSize, this.messageContainer).pipe(
            catchError(error => {
                // catch error, neu co error, hien thi popup thong bao, redirect ve 'home' page
                this.alertify.error('Problem retrieving messages');
                this.route.navigate(['/home']);
                // return observable null, su dung 'of'
                return of(null);
            })
        );
    }
}
