HUONG DAN GOI RELEASE KKSHOP
============================

1. Tao goi chay local
---------------------
- Tu thu muc goc project, chay PUBLISH_KKSHOP.bat.
- Script se publish website vao ReleasePackage\Publish.
- Script se copy Database\DATN_PCStore.sql sang ReleasePackage\Database\DATN_PCStore.sql neu file nguon ton tai.

2. Chay website da publish
--------------------------
- Chay ReleasePackage\START_KKSHOP.bat.
- Website mo tai http://localhost:5000.

3. Tat website
--------------
- Chay ReleasePackage\STOP_KKSHOP.bat.
- Hoac quay lai cua so dang chay website, bam Ctrl+C roi chon Y.

4. Luu y ve git
---------------
- Khong commit ReleasePackage\Publish.
- Khong commit ReleasePackage\Database\DATN_PCStore.sql.
- Khong commit file exe, dll, pdb, zip, bak, sql copy lon trong ReleasePackage.
- Chi commit cac file script/README va cac file .gitkeep can thiet.
