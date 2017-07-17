--SQL SERVER 2012 
DECLARE @counter as int = 0
DECLARE @mysubject as nvarchar(255) = 'test'

WHILE @counter < 100 
BEGIN
EXEC [EMAIL].[CLRSendMail]	@profileName = N'SimpleTalk'
							,@mailTo = N'darko.martinovic@outlook.com'
							,@configName =N'default'
							,@mailSubject = @mysubject
							,@fileAttachments = 'D:\Irata.Maintance\Log_2017_04_07_10_20_52.txt;c:\tmp\eDokumenti_tmp_3.pdf'
							,@mailBody = N'<blocked-process-report monitorLoop="181962">
  <blocked-process>
    <process id="process3ff013c38" taskpriority="0" logused="0" waitresource="PAGE: 5:1:5936864 " waittime="12111" ownerId="255870166" transactionname="SELECT" lasttranstarted="2017-05-26T12:57:43.530" XDES="0x9faf3ad10" lockMode="S" schedulerid="1" kpid="5904" status="suspended" spid="131" sbid="0" ecid="0" priority="0" trancount="0" lastbatchstarted="2017-05-26T12:57:43.103" lastbatchcompleted="2017-05-26T12:57:43.100" lastattention="1900-01-01T00:00:00.100" clientapp=".Net SqlClient Data Provider" hostname="SERVICE2" hostpid="2864" loginname="sa" isolationlevel="read committed (2)" xactid="255870166" currentdb="5" lockTimeout="4294967295" clientoption1="671088672" clientoption2="128056">
      <executionStack>
        <frame line="1" stmtstart="50" sqlhandle="0x02000000259c02263c40846f7de70ea52f9c8f3d8fdfc07a0000000000000000000000000000000000000000" />
        <frame line="1" sqlhandle="0x0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" />
      </executionStack>
      <inputbuf>
(@p0 int,@p1 int,@p2 int)select N0."StavkaRelacijeId",N0."ParentStavkaId",N0."ParentStavkaTipId",N0."ChildStavkaId",N0."ChildStavkaTipId",N0."StavkeRelacijeTip",N1."Redoslijed",N1."Naziv",N1."ParentOpis",N1."ChildOpis",N0."NapravioId",N0."NapravljenoDateTime",N0."Opis" from ("dbo"."StavkeRelacije" N0
 left join "dbo"."StavkeRelacijeTipovi" N1 on (N0."StavkeRelacijeTip" = N1."StavkeRelacijeTipId"))
where ((N0."ChildStavkaTipId" = @p0) and (N0."ChildStavkaId" = @p1) and (N0."ParentStavkaTipId" = @p2))   </inputbuf>
    </process>
  </blocked-process>
  <blocking-process>
    <process status="sleeping" spid="192" sbid="0" ecid="0" priority="0" trancount="1" lastbatchstarted="2017-05-26T12:57:55.257" lastbatchcompleted="2017-05-26T12:57:55.257" lastattention="1900-01-01T00:00:00.257" clientapp=".Net SqlClient Data Provider" hostname="SERVICE2" hostpid="2864" loginname="sa" isolationlevel="read committed (2)" xactid="255868238" currentdb="5" lockTimeout="4294967295" clientoption1="671088672" clientoption2="128056">
      <executionStack />
      <inputbuf>
(@p0 int,@p1 int,@p2 int,@p3 int,@p4 int,@p5 uniqueidentifier,@p6 datetime,@p7 int,@p8 int,@p9 int,@p10 int,@p11 int,@p12 int,@p13 uniqueidentifier,@p14 datetime,@p15 int,@p16 int,@p17 int,@p18 int,@p19 int,@p20 int,@p21 uniqueidentifier,@p22 datetime,@p23 int,@r int output)update "dbo"."StavkeRelacije" set "ParentStavkaId"=@p0,"ParentStavkaTipId"=@p1,"ChildStavkaId"=@p2,"ChildStavkaTipId"=@p3,"StavkeRelacijeTip"=@p4,"NapravioId"=@p5,"NapravljenoDateTime"=@p6,"Opis"=null where ("StavkaRelacijeId" = @p7) IF @@ROWCOUNT &lt;&gt; 1 begin set @r=0 RETURN end  update "dbo"."StavkeRelacije" set "ParentStavkaId"=@p8,"ParentStavkaTipId"=@p9,"ChildStavkaId"=@p10,"ChildStavkaTipId"=@p11,"StavkeRelacijeTip"=@p12,"NapravioId"=@p13,"NapravljenoDateTime"=@p14,"Opis"=null where ("StavkaRelacijeId" = @p15) IF @@ROWCOUNT &lt;&gt; 1 begin set @r=0 RETURN end  update "dbo"."StavkeRelacije" set "ParentStavkaId"=@p16,"ParentStavkaTipId"=@p17,"ChildStavkaId"=@p18,"ChildStavkaTipId"=@p19,"StavkeRelacijeTip"=@p20,"NapravioId"=@p21,"NapravljenoD   </inputbuf>
    </process>
  </blocking-process>
</blocked-process-report>';
SET @mysubject =  CAST(@counter AS NVARCHAR(10))
SET @counter = @counter + 1
END
