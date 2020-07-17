import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { FileUploader } from 'ng2-file-upload';
import { Photo } from 'src/app/_models/photo';
import { from } from 'rxjs';
import { environment } from 'src/environments/environment';
import { AuthService } from 'src/app/_services/auth.service';
import { UserService } from 'src/app/_services/user.service';
import { AlertifyService } from 'src/app/_services/alertify.service';

@Component({
  selector: 'app-photo-editor',
  templateUrl: './photo-editor.component.html',
  styleUrls: ['./photo-editor.component.css']
})
export class PhotoEditorComponent implements OnInit {
  @Input() photos: Photo[];
  // khai bao 1 bien output ma component con se phat ra va component cha se nhan
  // EventEmitter<string>: tra ve 1 string
  @Output() getMemberPhotoChange = new EventEmitter<string>();
  uploader: FileUploader;
  hasBaseDropZoneOver = false;
  baseUrl = environment.apiUrl;
  currentMain: Photo;

  constructor(private authService: AuthService, private userService: UserService,
    private alertify: AlertifyService) { }

  ngOnInit() {
    this.initializeUploader();
  }

  fileOverBase(e: any): void {
    this.hasBaseDropZoneOver = e;
  }

  initializeUploader() {
    this.uploader = new FileUploader({
      url: this.baseUrl + 'users/' + this.authService.decodedToken.nameid + '/photos',
      authToken: 'Bearer ' + localStorage.getItem('token'),
      isHTML5: true,
      allowedFileType: ['image'],
      // remove from upload queue after file uploaded
      removeAfterUpload: true,
      autoUpload: false,
      // max 10mb
      maxFileSize: 10 * 1024 * 1024
    });
    // file upload not going with credentials
    this.uploader.onAfterAddingFile = (file) => {file.withCredentials = false; };
    // neu upload success, server response item, response, status, headers
    this.uploader.onSuccessItem = (item, response, status, headers) => {
      if (response) {
        // convert string to json
        const res: Photo = JSON.parse(response);
        // create object de store data response sau khi da convert sang json
        const photo = {
          id: res.id,
          url: res.url,
          dateAdded: res.dateAdded,
          description: res.description,
          isMain: res.isMain,
          isApproved: res.isApproved
        };
        // push: add photo moi vao cuoi mang
        this.photos.push(photo);
        if (photo.isMain) {
          this.authService.changeMemberPhoto(photo.url);
          // thuc hien update lai gia tri luu trong localStorage
          this.authService.currentUser.photoUrl = photo.url;
          localStorage.setItem('user', JSON.stringify(this.authService.currentUser));
        }
      }
    };
  }

  setMainPhoto(photo: Photo) {
    this.userService.setMainPhoto(this.authService.decodedToken.nameid, photo.id).subscribe(() => {
      // filter trong 1 array se return ve 1 array chua cac phan tu thoa man dieu kien
      // lay phan tu dau tien nen la [0]
      this.currentMain = this.photos.filter(p => p.isMain === true)[0];
      this.currentMain.isMain = false;
      photo.isMain = true;
      // component photo-editor la child cua component member-editor, se phat ra (emit) url cua photo dc set la main
      // this.getMemberPhotoChange.emit(photo.url);
      // thuc hien thay doi gia tri photoUrl
      this.authService.changeMemberPhoto(photo.url);
      // thuc hien update lai gia tri luu trong localStorage
      this.authService.currentUser.photoUrl = photo.url;
      localStorage.setItem('user', JSON.stringify(this.authService.currentUser));
    }, error => {
      this.alertify.error(error);
    });
  }

  deletePhoto(id: number) {
    this.alertify.confirm('Are you sure you want to delete this photo?', () => {
      this.userService.deletePhoto(this.authService.decodedToken.nameid, id).subscribe(() => {
        // splice(): remove elements from array
        this.photos.splice(this.photos.findIndex(p => p.id === id), 1);
        this.alertify.success('Photo has been deleted');
      }, error => {
        this.alertify.error(error);
      });
    });
  }

}
