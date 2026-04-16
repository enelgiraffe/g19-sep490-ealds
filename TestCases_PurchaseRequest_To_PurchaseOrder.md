# Test Cases luong Yeu cau mua -> Don mua

TC01 - Tao yeu cau mua voi nhieu loai tai san
- Muc tieu: Truong phong tao duoc nhieu dong loai tai san.
- Buoc test:
  1. Mo form tao yeu cau mua.
  2. Nhap tieu de.
  3. Them 3 dong trong danh muc loai tai san de xuat.
  4. Moi dong chon loai tai san va nhap so luong.
  5. Nhap ly do de nghi, bam gui.
- Ket qua mong doi:
  1. Tao yeu cau thanh cong.
  2. Khong co truong nha cung cap de xuat.
  3. Khong co muc muc dich su dung.
  4. Luu dung 3 dong loai tai san va so luong.

TC02 - Validation bat buoc tren form yeu cau mua
- Muc tieu: Khong cho gui khi thieu thong tin chinh.
- Buoc test:
  1. De trong tieu de, bam gui.
  2. Co dong nhung chua chon loai tai san, bam gui.
  3. Nhap so luong = 0 hoac am.
- Ket qua mong doi:
  1. Bao loi bat buoc tieu de.
  2. Bao loi bat buoc chon loai tai san.
  3. Khong chap nhan so luong khong hop le.

TC03 - Xem chi tiet yeu cau mua theo cau truc moi
- Muc tieu: Man hinh xem dong bo voi form moi.
- Buoc test:
  1. Mo chi tiet yeu cau vua tao.
- Ket qua mong doi:
  1. Hien thi danh muc loai tai san va so luong.
  2. Khong hien thi nha cung cap de xuat.
  3. Khong hien thi muc dich su dung.
  4. Ly do de nghi hien thi dung noi dung.

TC04 - Tao don mua tu yeu cau mua
- Muc tieu: Don mua bi rang buoc dung so dong va dung so luong theo yeu cau.
- Buoc test:
  1. Mo form tao don mua.
  2. Chon ma yeu cau da co 3 dong loai tai san.
- Ket qua mong doi:
  1. Don mua hien thi dung 3 dong.
  2. Khong cho them dong hoac xoa dong khi da link yeu cau.
  3. So luong moi dong dung voi yeu cau va khong cho sua.

TC05 - Chi cho chon tai san dung loai tren tung dong
- Muc tieu: Khong chon sai loai tai san.
- Buoc test:
  1. O tung dong, bam chon tai san.
  2. Kiem tra danh sach tai san trong modal.
- Ket qua mong doi:
  1. Danh sach chi gom tai san cung loai voi dong yeu cau.
  2. Khong hien tai san khac loai.

TC06 - Chan submit neu lech so dong hoac so luong
- Muc tieu: Bao ve nghiep vu ngay ca khi payload bi sua.
- Buoc test:
  1. Gia lap gui payload co so dong khac so dong yeu cau.
  2. Gia lap gui payload co so luong mot dong khac so luong yeu cau.
- Ket qua mong doi:
  1. He thong chan submit.
  2. Hien thong bao dung nghiep vu:
     - Don mua phai giu dung so dong loai tai san nhu yeu cau mua.
     - So luong tung loai tai san phai dung theo yeu cau mua da chon.

TC07 - Ngay giao du kien o thong tin chung va tung dong
- Muc tieu: Van hanh theo co che mac dinh + dieu chinh rieng.
- Buoc test:
  1. Chon ngay giao du kien o thong tin chung.
  2. Kiem tra cac dong chua co ngay.
  3. Sua ngay rieng cho 1 dong.
  4. Doi ngay thong tin chung lan nua.
- Ket qua mong doi:
  1. Ngay o thong tin chung chi dien vao dong chua co ngay.
  2. Dong da sua ngay rieng khong bi ghi de.

TC08 - Don vi tinh dang dropdown + Khac
- Muc tieu: Dam bao du lieu chuan nhung van linh hoat.
- Buoc test:
  1. Chon don vi trong dropdown co san.
  2. Chon Khac va nhap don vi tuy chinh.
- Ket qua mong doi:
  1. Luu duoc don vi co san trong dropdown.
  2. Khi chon Khac thi hien o nhap va luu duoc don vi tuy chinh.
