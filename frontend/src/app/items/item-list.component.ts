import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { ItemService, Item } from './item.service';

@Component({
  selector: 'app-item-list',
  standalone: true,
  imports: [CommonModule, HttpClientModule],
  templateUrl: './item-list.component.html',
  styleUrls: ['./item-list.component.css']
})
export class ItemListComponent implements OnInit {
  items: Item[] = [];

  constructor(private itemService: ItemService) {}

  ngOnInit(): void {
    this.loadItems();
  }

  loadItems(): void {
    // In a real app with a backend, this would fetch from the service:
    // this.itemService.getItems().subscribe(data => this.items = data);
    
    // For demonstration, using dummy data:
    this.items = [
      { id: 1, name: 'Sample Item 1', description: 'This is a sample item' },
      { id: 2, name: 'Sample Item 2', description: 'Another sample item' }
    ];
  }

  deleteItem(id: number): void {
    // this.itemService.deleteItem(id).subscribe(() => this.loadItems());
    this.items = this.items.filter(item => item.id !== id);
  }
}
