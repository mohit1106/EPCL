import { Component, OnInit } from '@angular/core';

interface Document {
  id: string;
  entityType: string;
  documentType: string;
  fileName: string;
  fileSizeBytes: number;
  uploadDate: Date;
  uploadedByUserId: string;
  expiryDate?: Date;
  status: string;
}

@Component({
  selector: 'app-admin-documents',
  templateUrl: './documents.component.html',
  styleUrls: [],
})
export class AdminDocumentsComponent implements OnInit {
  documents: Document[] = [];
  isLoading = false;
  isUploadModalOpen = false;

  // Mock data for presentation
  ngOnInit() {
    this.loadDocuments();
  }

  loadDocuments() {
    this.isLoading = true;
    setTimeout(() => {
      this.documents = [
        {
          id: 'doc-101',
          entityType: 'Station',
          documentType: 'ExplosiveLicense',
          fileName: 'PESO_License_STN123.pdf',
          fileSizeBytes: 1048576,
          uploadDate: new Date('2025-10-12'),
          uploadedByUserId: 'user-xyz',
          expiryDate: new Date('2026-10-12'),
          status: 'Valid'
        },
        {
          id: 'doc-102',
          entityType: 'Driver',
          documentType: 'License',
          fileName: 'DL_MH12_4567.jpg',
          fileSizeBytes: 512000,
          uploadDate: new Date('2025-11-05'),
          uploadedByUserId: 'user-abc',
          expiryDate: new Date('2026-05-05'),
          status: 'Valid'
        },
        {
          id: 'doc-103',
          entityType: 'Tank',
          documentType: 'CalibrationChart',
          fileName: 'Tank4_Calib.pdf',
          fileSizeBytes: 2048000,
          uploadDate: new Date('2023-01-10'),
          uploadedByUserId: 'user-sys',
          expiryDate: new Date('2024-01-10'),
          status: 'Expired'
        }
      ];
      this.isLoading = false;
    }, 800);
  }

  openUploadModal() {
    this.isUploadModalOpen = true;
  }

  closeUploadModal() {
    this.isUploadModalOpen = false;
  }

  formatBytes(bytes: number, decimals = 2) {
    if (!+bytes) return '0 Bytes';
    const k = 1024;
    const dm = decimals < 0 ? 0 : decimals;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return `${parseFloat((bytes / Math.pow(k, i)).toFixed(dm))} ${sizes[i]}`;
  }

  getStatusClass(status: string) {
    return status === 'Valid' ? 'text-green-400 bg-green-400/10' : 'text-red-400 bg-red-400/10';
  }
}
