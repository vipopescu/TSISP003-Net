//||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
//||																															||
//||									RFM_MDL - RTA PROTOCOL FRAME MANAGER MODULE												||
//||																															||
//||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||

//	The RTA protocol frame manager module will be managing RTA protocol data/functionalities of variable speed signs 

//--------------------------------------------------	CONTENTS	------------------------------------------------------------//
//
//	INITIALISATION
//	COMMUNICATIONS STATUS
//	AVAILABILITY STATUS
//	SOCKET STATUS
//	SESSION STATUS
//	RESET LOGS CONTROLLER
//
//	STEP 0 ( DATA ANALYSES )
//		STEP 0 FIRST SCAND CYCLE	
//		STEP 0 CONTROL
//		STEP 0 CONDITIONS
//		STEP 0 TRANSITIONS	
//
//	STEP 1 ( DATA PROCESSING )
//		STEP 1 FIRST SCAND CYCLE	
//		STEP 1 CONTROL
//		STEP 1 CONDITIONS
//		STEP 1 TRANSITIONS	
//
//	STEP 2 ( CRC CALCULATION )
//		STEP 2 FIRST SCAND CYCLE	
//		STEP 2 CONTROL
//		STEP 2 CONDITIONS
//		STEP 2 TRANSITIONS		
//
//	STEP 3 ( PASSWORD BUILDER )
//		STEP 3 FIRST SCAND CYCLE	
//		STEP 3 CONTROL
//		STEP 3 CONDITIONS
//		STEP 3 TRANSITIONS	
//
//	STEP 4 ( CONVERSION 4 ASCII )
//		STEP 4 FIRST SCAND CYCLE	
//		STEP 4 CONTROL
//		STEP 0 CONDITIONS
//		STEP 4 TRANSITIONS	
//
//	STEP 5 ( SEND DATA PACKET )
//		STEP 5 FIRST SCAND CYCLE	
//		STEP 5 CONTROL
//		STEP 5 CONDITIONS
//		STEP 5 TRANSITIONS	
//
//	STEP 6 ( RECEIVED DATA PACKET )
//		STEP 6 FIRST SCAND CYCLE	
//		STEP 6 CONTROL
//		STEP 6 CONDITIONS
//		STEP 6 TRANSITIONS	
//	
//	STEP 7 ( DATA EXTRACTION )
//		STEP 7 FIRST SCAND CYCLE	
//		STEP 7 CONTROL
//		STEP 7 CONDITIONS
//		STEP 7 TRANSITIONS	
//
//	EVENTS TRIGGER
//		FAULT
//		ALARM	
//	HEALTH STATUS
//	RESET COMMANDS ( FROM SCADA )
//
//------------------------------------------------------------------------------------------------------------------------------//

//--------------------------------------------------	MODULE LOGIC	--------------------------------------------------------//

//	INITIALISATION
	IF CURR_STEP < 0 OR CURR_STEP > 7 THEN CURR_STEP := 0; END_IF;
//	Heart beat delay trigger
	TMR[0].PRE := 3000;	
	TMR[0].TimerEnable := DEVICES_CTRL_MI.APP_REQ = 0 AND CURR_STEP = 0;
	TONR(TMR[0]);	
	IF TMR[0].DN THEN DEVICES_CTRL_MI.APP_REQ := 7; END_IF;
//	Update time trigger
	TMR[1].PRE := 60000;
	TMR[1].TimerEnable := UPDATE_TIME_CFG AND DEVICES_CTRL_MI.APP_REQ <> 12; 
	TONR(TMR[1]);	
	IF TMR[1].DN AND DEVICES_CTRL_MI.APP_REQ = 0 THEN 
		DEVICES_CTRL_MI.APP_REQ := 12; 
	END_IF;

//	COMMUNICATIONS STATUS
	IF TCD_MDL_STS_MI.29 THEN
		DEVICE_STS.29 := 1;																			// SET VALUE
	ELSE
		DEVICE_STS.29 := 0;																			// RESET VALUE
		DEVICE_STS.5 := 0;																			// RESET VALUE
	END_IF;

//	AVAILABILITY STATUS
	IF DEVICE_CMD.0 OR 
	  (NOT DEVICE_STS.0 AND NOT DEVICE_STS.1 AND NOT DEVICE_STS.2) THEN 
		DEVICE_STS.0 := 1;																			// SET AVAILABLE STATUS
		DEVICE_STS.1 := 0;																			// RESET UNAVAILABLE STATUS
		DEVICE_STS.2 := 0;																			// RESET MAINTENANCE STATUS		
	ELSIF DEVICE_CMD.1 THEN
		DEVICE_STS.0 := 0;																			// RESET AVAILABLE STATUS
		DEVICE_STS.1 := 1;																			// SET UNAVAILABLE STATUS
		DEVICE_STS.2 := 0;																			// RESET MAINTENANCE STATUS	
	ELSIF DEVICE_CMD.2 THEN
		DEVICE_STS.0 := 0;																			// RESET AVAILABLE STATUS
		DEVICE_STS.1 := 0;																			// RESET UNAVAILABLE STATUS
		DEVICE_STS.2 := 1;																			// SET MAINTENANCE STATUS	
	END_IF;

//	SOCKET STATUS
//	Socket connected ( 0 : Disconnected / 1 : Connected )
	DEVICE_STS.4 := TCD_MDL_STS_MI.4;
//	Port connection
//	None
	DEVICE_STS.7 := TCD_MDL_STS_MI.6;
//	Primary 
	DEVICE_STS.8 := TCD_MDL_STS_MI.7;
//	Secondary 
	DEVICE_STS.9 := TCD_MDL_STS_MI.8;
	
//	SESSION STATUS
//	Session status memory
	IF DEVICE_STS.5 AND DEVICE_CMD.6 THEN 
		CMD_SES_MEM := 1;																			// COMMAND LOG OUT MEMORY
	ELSIF NOT DEVICE_STS.5 AND DEVICE_CMD.5 THEN 
		CMD_SES_MEM := 0;																			// COMMAND LOG IN MEMORY
	END_IF;
//	Set session log out application message
	IF SYS_MODE_MI = 0 THEN
		IF DEVICES_CTRL_MI.APP_REQ = 0 AND CMD_SES_MEM AND DEVICE_STS.5 THEN
			DEVICES_CTRL_MI.APP_REQ := 8;
		END_IF;
	ELSE 
		DEVICE_STS.5 := CMD_SES_MEM;
	END_IF;

//	RESET LOGS CONTROLLER
//	Reset alarm logs memory
	IF DEVICE_CMD.8 THEN
		RST_SYS_MEM := 1;
	END_IF;


// STEP 0 ( DATA ANALYSES )
IF CURR_STEP = 0 THEN
	
//	STEP 0 FIRST SCAND CYCLE
	IF CURR_STEP <> LAST_STEP THEN 
		STEP_COND[CURR_STEP] := 0;													// RESET VALUE
		STEP_TRANS[CURR_STEP] := 0;													// RESET VALUE
		CURR_MI_CODE.CHR_1 := 48;													// RESET VALUE
		CURR_MI_CODE.CHR_2 := 48;													// RESET VALUE
		FOR i:= 0 TO PACKET_CMD_MI.CMD.LEN -1 DO 										// RESET VALUES
			PACKET_CMD_MI.CMD.DATA[i] := 48;
		END_FOR;
		FOR i:= 0 TO PACKET_LOCAL.LEN -1 DO 													// RESET VALUES
			PACKET_LOCAL.DATA[i] := 48;
		END_FOR;
		PACKET_LOCAL.LEN := 0;														// RESET VALUE
		PACKET_CMD_MI.CMD.LEN := 0;														// RESET VALUE	
		IF TRANS_TYPE_LOCAL = 1 THEN DEVICES_CTRL_MI.APP_REQ := 0; END_IF;
		TRANS_TYPE_LOCAL := 0;
		LAST_STEP := CURR_STEP;														// SET VALUE
	END_IF;
	
//	STEP 0 CONTROL
//	PLC mode (Production)
	IF SYS_MODE_MI = 0 THEN
//		Session logged out
		IF (NOT DEVICE_STS.5 AND TCD_MDL_STS_MI.29 AND NOT CMD_SES_MEM) THEN
//			Start session application message
			IF PACKET_FDBK_MI.DATA[9] <> MI_CODE_CFG[3].CHR_2 THEN
				CURR_MI_CODE.CHR_1 := MI_CODE_CFG[2].CHR_1;								// SET VALUE
				CURR_MI_CODE.CHR_2 := MI_CODE_CFG[2].CHR_2;								// SET VALUE
				PACKET_LOCAL.LEN := 10;													// SET VALUE															
				TRANS_TYPE_LOCAL := 2;
			END_IF;
//			Password application message 
			IF PACKET_FDBK_MI.DATA[8] = MI_CODE_CFG[3].CHR_1 AND 
			   PACKET_FDBK_MI.DATA[9] = MI_CODE_CFG[3].CHR_2 THEN 
				CURR_MI_CODE.CHR_1 := MI_CODE_CFG[4].CHR_1;								// SET VALUE
				CURR_MI_CODE.CHR_2 := MI_CODE_CFG[4].CHR_2;								// SET VALUE
				PACKET_LOCAL.LEN := 10;													// SET VALUE															
				TRANS_TYPE_LOCAL := 2;
			END_IF;	
		END_IF;
//		Session logged in
		IF DEVICE_STS.5 THEN
//			Sign display frame application message 
			IF DEVICES_CTRL_MI.APP_REQ = 1 THEN
				CURR_MI_CODE.CHR_1 := MI_CODE_CFG[15].CHR_1;							// SET VALUE
				CURR_MI_CODE.CHR_2 := MI_CODE_CFG[15].CHR_2;							// SET VALUE
				DEVICE_MI_LOCAL.GROUP_ID := DEVICES_CTRL_MI.GROUP_ID;							// SET VALUE
				DEVICE_MI_LOCAL.DEVICES[0].CMD := DEVICES_CTRL_MI.DEVICES[0].CMD;						// SET VALUE
				PACKET_LOCAL.LEN := 14;													// SET VALUE															// SET VALUE
				TRANS_TYPE_LOCAL := 2;
			END_IF;	
//			Sign display atomics frames application message 
			IF DEVICES_CTRL_MI.APP_REQ = 2 THEN
				CURR_MI_CODE.CHR_1 := MI_CODE_CFG[13].CHR_1;							// SET VALUE
				CURR_MI_CODE.CHR_2 := MI_CODE_CFG[13].CHR_2;							// SET VALUE
				DEVICE_MI_LOCAL.GROUP_ID := DEVICES_CTRL_MI.GROUP_ID;							// SET VALUE
				DEVICE_MI_LOCAL.NUM_SIGNS_GROUP := DEVICES_CTRL_MI.NUM_SIGNS_GROUP;					// SET VALUE
				PACKET_LOCAL.LEN := 14;													// SET VALUE															// SET VALUE
				TRANS_TYPE_LOCAL := 2;
				FOR i:= 0 TO DEVICE_MI_LOCAL.NUM_SIGNS_GROUP - 1 DO						// SET VALUES
					DEVICE_MI_LOCAL.DEVICES[i].ID := DEVICES_CTRL_MI.DEVICES[i].ID;			// SET VALUE
					DEVICE_MI_LOCAL.DEVICES[i].CMD := DEVICES_CTRL_MI.DEVICES[i].CMD;			// SET VALUE
					PACKET_LOCAL.LEN := PACKET_LOCAL.LEN + 4;							// SET VALUE						
				END_FOR;
			END_IF;	
//			Retrieve Fault Log application message
			IF DEVICES_CTRL_MI.APP_REQ = 3 THEN 
				CURR_MI_CODE.CHR_1 := MI_CODE_CFG[20].CHR_1;								// SET VALUE
				CURR_MI_CODE.CHR_2 := MI_CODE_CFG[20].CHR_2;								// SET VALUE
				PACKET_LOCAL.LEN := 10;													// SET VALUE															// SET VALUE
				TRANS_TYPE_LOCAL := 2;
				FOR i:= 0 TO 19 DO
					EV_REG_IN.REG[i].TYPE := 0;												// RESET VALUE
					EV_REG_IN.REG[i].ER_CODE := 0;												// RESET VALUE
					EV_REG_IN.REG[i].STS := 0;												// RESET VALUE				
				END_FOR;
			END_IF;
//			System reset application message
			IF (DEVICES_CTRL_MI.APP_REQ = 4) OR (DEVICES_CTRL_MI.APP_REQ = 0 AND RST_SYS_MEM) THEN
				DEVICES_CTRL_MI.APP_REQ := 4;											// SET VALUE
				CURR_MI_CODE.CHR_1 := MI_CODE_CFG[8].CHR_1;								// SET VALUE
				CURR_MI_CODE.CHR_2 := MI_CODE_CFG[8].CHR_2;								// SET VALUE
				IF RST_SYS_MEM THEN
					DEVICE_MI_LOCAL.GROUP_ID := 0;										// SET VALUE
					RST_SYS_MEM := 0;
				ELSE	
					DEVICE_MI_LOCAL.GROUP_ID := DEVICES_CTRL_MI.GROUP_ID;				// SET VALUE
				END_IF;
				PACKET_LOCAL.LEN := 14;													// SET VALUE															// SET VALUE
				TRANS_TYPE_LOCAL := 1;
			END_IF;
//			Sign set dimming level application message
			IF DEVICES_CTRL_MI.APP_REQ = 5 THEN 
				CURR_MI_CODE.CHR_1 := MI_CODE_CFG[17].CHR_1;							// SET VALUE
				CURR_MI_CODE.CHR_2 := MI_CODE_CFG[17].CHR_2;							// SET VALUE
				DEVICE_MI_LOCAL.GROUP_ID := DEVICES_CTRL_MI.GROUP_ID;					// SET VALUE
				DEVICE_MI_LOCAL.DEVICES[0].DIM_LEVEL := DEVICES_CTRL_MI.DEVICES[0].DIM_LEVEL;
				PACKET_LOCAL.LEN := 18;													// SET VALUE															// SET VALUE
				TRANS_TYPE_LOCAL := 1;
			END_IF;
//			Power on/off application message
			IF DEVICES_CTRL_MI.APP_REQ = 6 THEN 
				CURR_MI_CODE.CHR_1 := MI_CODE_CFG[18].CHR_1;												// SET VALUE
				CURR_MI_CODE.CHR_2 := MI_CODE_CFG[18].CHR_2;												// SET VALUE
				DEVICE_MI_LOCAL.GROUP_ID := DEVICES_CTRL_MI.GROUP_ID;										// SET VALUE
				DEVICE_MI_LOCAL.DEVICES[0].POWER := DEVICES_CTRL_MI.DEVICES[0].POWER;						// SET VALUE
				PACKET_LOCAL.LEN := 16;																		// SET VALUE
				TRANS_TYPE_LOCAL := 1;			
			END_IF;				
//			Heart beat application message 
			IF DEVICES_CTRL_MI.APP_REQ = 7 THEN
				CURR_MI_CODE.CHR_1 := MI_CODE_CFG[5].CHR_1;								// SET VALUE
				CURR_MI_CODE.CHR_2 := MI_CODE_CFG[5].CHR_2;								// SET VALUE
				PACKET_LOCAL.LEN := 10;													// SET VALUE															// SET VALUE
				TRANS_TYPE_LOCAL := 2;
			END_IF;	
//			End session application message
			IF DEVICES_CTRL_MI.APP_REQ = 8 THEN 
				CURR_MI_CODE.CHR_1 := MI_CODE_CFG[7].CHR_1;								// SET VALUE
				CURR_MI_CODE.CHR_2 := MI_CODE_CFG[7].CHR_2;								// SET VALUE
				PACKET_LOCAL.LEN := 10;													// SET VALUE															// SET VALUE
				TRANS_TYPE_LOCAL := 2;				
			END_IF;
//			Disable/enable device application message
			IF DEVICES_CTRL_MI.APP_REQ = 9 THEN 
				CURR_MI_CODE.CHR_1 := MI_CODE_CFG[19].CHR_1;												// SET VALUE
				CURR_MI_CODE.CHR_2 := MI_CODE_CFG[19].CHR_2;												// SET VALUE
				DEVICE_MI_LOCAL.GROUP_ID := DEVICES_CTRL_MI.GROUP_ID;										// SET VALUE
				DEVICE_MI_LOCAL.DEVICES[0].DEVICE_FUNCT := DEVICES_CTRL_MI.DEVICES[0].DEVICE_FUNCT;			// SET VALUE			
				PACKET_LOCAL.LEN := 16;																		// SET VALUE	
				TRANS_TYPE_LOCAL := 1;			
			END_IF;
//			Sign request stored message application message
			IF DEVICES_CTRL_MI.APP_REQ = 10 THEN
				DEVICE_MI_LOCAL.REQ_STORED_TYPE := 1;												// SET VALUE
				FOR x := 5 TO 0 BY -1 DO
					IF MSG_CFG_LOCAL[x] <> 0 THEN
						DEVICE_MI_LOCAL.REQ_STORED_ID := MSG_CFG_LOCAL[x];					// SET VALUE
						CUR_MSG_INDEX_POS := x;														// SET VALUE
						MSG_CFG_LOCAL[x] := 0;												// RESET VALUE
						EXIT;
					END_IF;
				END_FOR;
				CURR_MI_CODE.CHR_1 := MI_CODE_CFG[22].CHR_1;										// SET VALUE
				CURR_MI_CODE.CHR_2 := MI_CODE_CFG[22].CHR_2;										// SET VALUE
				PACKET_LOCAL.LEN := 14;																// SET VALUE
				TRANS_TYPE_LOCAL := 2;
			END_IF;
//			Reset fault log application message
			IF DEVICES_CTRL_MI.APP_REQ = 0 AND RST_LOG_MEM THEN 
				DEVICES_CTRL_MI.APP_REQ := 11;
				CURR_MI_CODE.CHR_1 := MI_CODE_CFG[23].CHR_1;							// SET VALUE
				CURR_MI_CODE.CHR_2 := MI_CODE_CFG[23].CHR_2;							// SET VALUE
				PACKET_LOCAL.LEN := 10;													// SET VALUE															// SET VALUE
				TRANS_TYPE_LOCAL := 1;		
				RST_LOG_MEM := 0;		
			END_IF;
//			Update time application message
			IF DEVICES_CTRL_MI.APP_REQ = 12 THEN  
				CURR_MI_CODE.CHR_1 := MI_CODE_CFG[9].CHR_1;							// SET VALUE
				CURR_MI_CODE.CHR_2 := MI_CODE_CFG[9].CHR_2;							// SET VALUE
				DATE_TIME_LOCAL.Day := DATE_TIME_MI.Day;							// SET VALUE
				DATE_TIME_LOCAL.Month := DATE_TIME_MI.Month;						// SET VALUE
				DATE_TIME_LOCAL.Year := DATE_TIME_MI.Year;							// SET VALUE
				DATE_TIME_LOCAL.Hour := DATE_TIME_MI.Hour;							// SET VALUE
				DATE_TIME_LOCAL.Minute := DATE_TIME_MI.Minute;						// SET VALUE
				DATE_TIME_LOCAL.Second := DATE_TIME_MI.Second;						// SET VALUE
				PACKET_LOCAL.LEN := 24;												// SET VALUE
				TRANS_TYPE_LOCAL := 1;
			END_IF;
//			Ready to recieve a command
			DEVICE_STS.6 := 1;		
		END_IF;
	ELSE 
//		End session status
		IF CMD_SES_MEM OR NOT DEVICE_STS.4 THEN 
			DEVICE_STS.5 := 0;				// SESSION LOGGED OUT
		ELSE
			DEVICE_STS.5 := 1;				// SESSION LOGGED IN
		END_IF;
	END_IF;
	
//	STEP 0 CONDITIONS
//	Conditions triggers
	STEP_COND[CURR_STEP].0 := PACKET_LOCAL.LEN <> 0;								// PACKET SELECTED
		
//	STEP 0 TRANSITIONS	
//	Transitions triggers
	STEP_TRANS[CURR_STEP].0 := STEP_COND[CURR_STEP].0;
//	Transition ( DATA ANALYSES to DATA PROCESSING )	
	IF STEP_TRANS[CURR_STEP].0 THEN
		CURR_STEP := 1;																// SET VALUE
	END_IF;	
		
END_IF;

// STEP 1 ( DATA PROCESSING )
IF CURR_STEP = 1 THEN
	
//	STEP 1 FIRST SCAND CYCLE		
	IF CURR_STEP <> LAST_STEP THEN 
		STEP_COND[CURR_STEP] := 0;													// RESET VALUE
		STEP_TRANS[CURR_STEP] := 0;													// RESET VALUE
		DEVICE_STS.6 := 0;															// RESET VALUE
//		Prepare standard packet part to be sent
		PACKET_LOCAL.DATA[0] := 1;													// SET VALUE
		PACKET_LOCAL.DATA[1] := CYCLE_POS_CHR_1;									// SET VALUE
		PACKET_LOCAL.DATA[2] := CYCLE_POS_CHR_2;									// SET VALUE
		PACKET_LOCAL.DATA[3] := CYCLE_POS_CHR_1;									// SET VALUE
		PACKET_LOCAL.DATA[4] := CYCLE_POS_CHR_2;									// SET VALUE
		DEC_1_CHR := CTRLLR_ID_CFG AND 15;													// SET VALUE
		DEC_2_CHR := (CTRLLR_ID_CFG AND 240)/16;											// SET VALUE
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_1_CHR THEN
				PACKET_LOCAL.DATA[6] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;	
		FOR i:= 0 TO 15 DO															// SET VALUE
			IF i = DEC_2_CHR THEN
				PACKET_LOCAL.DATA[5] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;
		PACKET_LOCAL.DATA[7] := 2;													// SET VALUE
		PACKET_LOCAL.DATA[8] := CURR_MI_CODE.CHR_1;									// SET VALUE
		PACKET_LOCAL.DATA[9] := CURR_MI_CODE.CHR_2;									// SET VALUE	
		LAST_STEP := CURR_STEP;														// SET VALUE
	END_IF;
	
//	STEP 1 CONTROL
//	Sign display frame packet
	IF CURR_MI_CODE.CHR_1 = MI_CODE_CFG[15].CHR_1 AND 
	   CURR_MI_CODE.CHR_2 = MI_CODE_CFG[15].CHR_2 THEN
//		Group ID
		DEC_1_CHR := DEVICE_MI_LOCAL.GROUP_ID AND 15;								// SET VALUE
		DEC_2_CHR := (DEVICE_MI_LOCAL.GROUP_ID AND 240)/16;							// SET VALUE
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_1_CHR THEN
				PACKET_LOCAL.DATA[11] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;	
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_2_CHR THEN
				PACKET_LOCAL.DATA[10] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;
//		Frame ID
		DEC_1_CHR := DEVICE_MI_LOCAL.DEVICES[0].CMD AND 15;							// SET VALUE
		DEC_2_CHR := (DEVICE_MI_LOCAL.DEVICES[0].CMD AND 240)/16;					// SET VALUE
		FOR x:= 0 TO 15 DO 															// SET VALUE
			IF x = DEC_1_CHR THEN
				PACKET_LOCAL.DATA[13] := ASCII_CHR_CFG[x];
				EXIT;
			END_IF;
		END_FOR;	
		FOR x:= 0 TO 15 DO 															// SET VALUE
			IF x = DEC_2_CHR THEN
				PACKET_LOCAL.DATA[12] := ASCII_CHR_CFG[x];
				EXIT;
			END_IF;
		END_FOR;	
	END_IF;
//	Sign display atomics frames packet
	IF CURR_MI_CODE.CHR_1 = MI_CODE_CFG[13].CHR_1 AND 
	   CURR_MI_CODE.CHR_2 = MI_CODE_CFG[13].CHR_2 THEN
//		Group ID
		DEC_1_CHR := DEVICE_MI_LOCAL.GROUP_ID AND 15;								// SET VALUE
		DEC_2_CHR := (DEVICE_MI_LOCAL.GROUP_ID AND 240)/16;							// SET VALUE
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_1_CHR THEN
				PACKET_LOCAL.DATA[11] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;	
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_2_CHR THEN
				PACKET_LOCAL.DATA[10] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;	
//		Number signs in the group
		DEC_1_CHR := DEVICE_MI_LOCAL.NUM_SIGNS_GROUP AND 15;						// SET VALUE
		DEC_2_CHR := (DEVICE_MI_LOCAL.NUM_SIGNS_GROUP AND 240)/16;					// SET VALUE
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_1_CHR THEN
				PACKET_LOCAL.DATA[13] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;	
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_2_CHR THEN
				PACKET_LOCAL.DATA[12] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;	
		z := 14;
//		Sign ID
		FOR i := 0 TO DEVICE_MI_LOCAL.NUM_SIGNS_GROUP -1 DO
			DEC_1_CHR := DEVICE_MI_LOCAL.DEVICES[i].ID AND 15;						// SET VALUE
			DEC_2_CHR := (DEVICE_MI_LOCAL.DEVICES[i].ID AND 240)/16;				// SET VALUE
			FOR x:= 0 TO 15 DO 														// SET VALUE
				IF x = DEC_1_CHR THEN
					PACKET_LOCAL.DATA[z+1] := ASCII_CHR_CFG[x];
					EXIT;
				END_IF;
			END_FOR;	
			FOR x:= 0 TO 15 DO 														// SET VALUE
				IF x = DEC_2_CHR THEN
					PACKET_LOCAL.DATA[z] := ASCII_CHR_CFG[x];
					EXIT;
				END_IF;
			END_FOR;
//		Frame ID
			DEC_1_CHR := DEVICE_MI_LOCAL.DEVICES[i].CMD AND 15;						// SET VALUE
			DEC_2_CHR := (DEVICE_MI_LOCAL.DEVICES[i].CMD AND 240)/16;				// SET VALUE
			FOR x:= 0 TO 15 DO 														// SET VALUE
				IF x = DEC_1_CHR THEN
					PACKET_LOCAL.DATA[z+3] := ASCII_CHR_CFG[x];
					EXIT;
				END_IF;
			END_FOR;	
			FOR x:= 0 TO 15 DO 														// SET VALUE
				IF x = DEC_2_CHR THEN
					PACKET_LOCAL.DATA[z+2] := ASCII_CHR_CFG[x];
					EXIT;
				END_IF;
			END_FOR;					
			z := z + 4;	
		END_FOR;		
	END_IF;
//	System Reset packet
	IF CURR_MI_CODE.CHR_1 = MI_CODE_CFG[8].CHR_1 AND 
	   CURR_MI_CODE.CHR_2 = MI_CODE_CFG[8].CHR_2 THEN
//		Group ID
		DEC_1_CHR := DEVICE_MI_LOCAL.GROUP_ID AND 15;								// SET VALUE
		DEC_2_CHR := (DEVICE_MI_LOCAL.GROUP_ID AND 240)/16;							// SET VALUE
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_1_CHR THEN
				PACKET_LOCAL.DATA[11] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;	
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_2_CHR THEN
				PACKET_LOCAL.DATA[10] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;
//		Set level two ( Blank display / Reset faults log )
		PACKET_LOCAL.DATA[12] := 48;												// SET VALUE
		PACKET_LOCAL.DATA[13] := 50;												// SET VALUE
	END_IF;
//	Sign set dimming level packet
	IF CURR_MI_CODE.CHR_1 = MI_CODE_CFG[17].CHR_1 AND 
	   CURR_MI_CODE.CHR_2 = MI_CODE_CFG[17].CHR_2 THEN
//		Groups numbers
		PACKET_LOCAL.DATA[10] := 48;
		PACKET_LOCAL.DATA[11] := 49;	   
//		Group ID
		DEC_1_CHR := DEVICE_MI_LOCAL.GROUP_ID AND 15;								// SET VALUE
		DEC_2_CHR := (DEVICE_MI_LOCAL.GROUP_ID AND 240)/16;							// SET VALUE
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_1_CHR THEN
				PACKET_LOCAL.DATA[13] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;	
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_2_CHR THEN
				PACKET_LOCAL.DATA[12] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;
//		Dimming mode ( 0 : Auto / 1 : Manual )
		IF DEVICE_MI_LOCAL.DEVICES[0].DIM_LEVEL = 0 THEN
			PACKET_LOCAL.DATA[14] := 48;
			PACKET_LOCAL.DATA[15] := 48;
			PACKET_LOCAL.DATA[16] := 48;
			PACKET_LOCAL.DATA[17] := 48;
		ELSE
			PACKET_LOCAL.DATA[14] := 48;
			PACKET_LOCAL.DATA[15] := 49;
//			Dimming level
			DEC_1_CHR := DEVICE_MI_LOCAL.DEVICES[0].DIM_LEVEL AND 15;								// SET VALUE
			DEC_2_CHR := (DEVICE_MI_LOCAL.DEVICES[0].DIM_LEVEL AND 240)/16;							// SET VALUE
			FOR i:= 0 TO 15 DO 															// SET VALUE
				IF i = DEC_1_CHR THEN
					PACKET_LOCAL.DATA[17] := ASCII_CHR_CFG[i];
					EXIT;
				END_IF;
			END_FOR;	
			FOR i:= 0 TO 15 DO 															// SET VALUE
				IF i = DEC_2_CHR THEN
					PACKET_LOCAL.DATA[16] := ASCII_CHR_CFG[i];
					EXIT;
				END_IF;
			END_FOR;	
		END_IF;	
	END_IF;
//	Power on/off packet
	IF CURR_MI_CODE.CHR_1 = MI_CODE_CFG[18].CHR_1 AND 
	   CURR_MI_CODE.CHR_2 = MI_CODE_CFG[18].CHR_2 THEN
//		Groups numbers
		PACKET_LOCAL.DATA[10] := 48;
		PACKET_LOCAL.DATA[11] := 49;   
//		Group ID
		DEC_1_CHR := DEVICE_MI_LOCAL.GROUP_ID AND 15;								// SET VALUE
		DEC_2_CHR := (DEVICE_MI_LOCAL.GROUP_ID AND 240)/16;							// SET VALUE
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_1_CHR THEN
				PACKET_LOCAL.DATA[13] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;	
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_2_CHR THEN
				PACKET_LOCAL.DATA[12] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;
//		Power command ( 0 : Off / 1 : On )
		DEC_1_CHR := DEVICE_MI_LOCAL.DEVICES[0].POWER AND 15;								// SET VALUE
		DEC_2_CHR := (DEVICE_MI_LOCAL.DEVICES[0].POWER AND 240)/16;							// SET VALUE
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_1_CHR THEN
				PACKET_LOCAL.DATA[15] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;	
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_2_CHR THEN
				PACKET_LOCAL.DATA[14] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;		
	END_IF;
//	Disable/enable device packet
	IF CURR_MI_CODE.CHR_1 = MI_CODE_CFG[19].CHR_1 AND 
	   CURR_MI_CODE.CHR_2 = MI_CODE_CFG[19].CHR_2 THEN
//		Groups numbers
		PACKET_LOCAL.DATA[10] := 48;
		PACKET_LOCAL.DATA[11] := 49;   
//		Group ID
		DEC_1_CHR := DEVICE_MI_LOCAL.GROUP_ID AND 15;								// SET VALUE
		DEC_2_CHR := (DEVICE_MI_LOCAL.GROUP_ID AND 240)/16;							// SET VALUE
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_1_CHR THEN
				PACKET_LOCAL.DATA[13] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;	
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_2_CHR THEN
				PACKET_LOCAL.DATA[12] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;
//		Power command ( 0 : Off / 1 : On )
		DEC_1_CHR := DEVICE_MI_LOCAL.DEVICES[0].DEVICE_FUNCT AND 15;				// SET VALUE
		DEC_2_CHR := (DEVICE_MI_LOCAL.DEVICES[0].DEVICE_FUNCT AND 240)/16;			// SET VALUE
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_1_CHR THEN
				PACKET_LOCAL.DATA[15] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;	
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_2_CHR THEN
				PACKET_LOCAL.DATA[14] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;		
	END_IF;
//	Sign request stored message application message
	IF CURR_MI_CODE.CHR_1 = MI_CODE_CFG[22].CHR_1 AND 
	   CURR_MI_CODE.CHR_2 = MI_CODE_CFG[22].CHR_2 THEN
		DEC_1_CHR := DEVICE_MI_LOCAL.REQ_STORED_TYPE AND 15;									// SET VALUE
		DEC_2_CHR := (DEVICE_MI_LOCAL.REQ_STORED_TYPE AND 240)/16;								// SET VALUE
		FOR i:= 0 TO 15 DO 																		// SET VALUE
			IF i = DEC_1_CHR THEN
				PACKET_LOCAL.DATA[11] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;				
		FOR i:= 0 TO 15 DO 																		// SET VALUE
			IF i = DEC_2_CHR THEN
				PACKET_LOCAL.DATA[10] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;
		DEC_1_CHR := DEVICE_MI_LOCAL.REQ_STORED_ID AND 15;										// SET VALUE
		DEC_2_CHR := (DEVICE_MI_LOCAL.REQ_STORED_ID AND 240)/16;								// SET VALUE
		FOR x:= 0 TO 15 DO 																		// SET VALUE
			IF x = DEC_1_CHR THEN			
				PACKET_LOCAL.DATA[13] := ASCII_CHR_CFG[x];
				EXIT;
			END_IF;
		END_FOR;	
		FOR x:= 0 TO 15 DO 																		// SET VALUE
			IF x = DEC_2_CHR THEN
				PACKET_LOCAL.DATA[12] := ASCII_CHR_CFG[x];
				EXIT;
			END_IF;
		END_FOR;	   
	END_IF;
//	Reset fault log application message	
	IF CURR_MI_CODE.CHR_1 = MI_CODE_CFG[9].CHR_1 AND 
	   CURR_MI_CODE.CHR_2 = MI_CODE_CFG[9].CHR_2 THEN
//		Day
		DEC_1_CHR := DATE_TIME_LOCAL.Day AND 15;									// SET VALUE
		DEC_2_CHR := (DATE_TIME_LOCAL.Day AND 240)/16;								// SET VALUE
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_1_CHR THEN
				PACKET_LOCAL.DATA[11] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;				
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_2_CHR THEN
				PACKET_LOCAL.DATA[10] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;
//		Month
		DEC_1_CHR := DATE_TIME_LOCAL.Month AND 15;									// SET VALUE
		DEC_2_CHR := (DATE_TIME_LOCAL.Month AND 240)/16;							// SET VALUE
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_1_CHR THEN
				PACKET_LOCAL.DATA[13] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;				
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_2_CHR THEN
				PACKET_LOCAL.DATA[12] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;
//		Year
		DEC_1_CHR := DATE_TIME_LOCAL.Year AND 15;									// SET VALUE
		DEC_2_CHR := (DATE_TIME_LOCAL.Year AND 240)/16;								// SET VALUE
		DEC_3_CHR := (DATE_TIME_LOCAL.Year AND 3840)/256;							// SET VALUE
		DEC_4_CHR := (DATE_TIME_LOCAL.Year AND 61440)/4096;							// SET VALUE
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_1_CHR THEN
				PACKET_LOCAL.DATA[17] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;				
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_2_CHR THEN
				PACKET_LOCAL.DATA[16] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;			
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_3_CHR THEN
				PACKET_LOCAL.DATA[15] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;			
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_4_CHR THEN
				PACKET_LOCAL.DATA[14] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;	
//		Hours
		DEC_1_CHR := DATE_TIME_LOCAL.Hour AND 15;									// SET VALUE
		DEC_2_CHR := (DATE_TIME_LOCAL.Hour AND 240)/16;								// SET VALUE
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_1_CHR THEN
				PACKET_LOCAL.DATA[19] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;				
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_2_CHR THEN
				PACKET_LOCAL.DATA[18] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;
//		Minutes
		DEC_1_CHR := DATE_TIME_LOCAL.Minute AND 15;									// SET VALUE
		DEC_2_CHR := (DATE_TIME_LOCAL.Minute AND 240)/16;							// SET VALUE
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_1_CHR THEN
				PACKET_LOCAL.DATA[21] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;				
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_2_CHR THEN
				PACKET_LOCAL.DATA[20] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;
//		Seconds
		DEC_1_CHR := DATE_TIME_LOCAL.Second AND 15;									// SET VALUE
		DEC_2_CHR := (DATE_TIME_LOCAL.Second AND 240)/16;							// SET VALUE
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_1_CHR THEN
				PACKET_LOCAL.DATA[23] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;				
		FOR i:= 0 TO 15 DO 															// SET VALUE
			IF i = DEC_2_CHR THEN
				PACKET_LOCAL.DATA[22] := ASCII_CHR_CFG[i];
				EXIT;
			END_IF;
		END_FOR;				   
	END_IF;
//	STEP 1 CONDITIONS
//	Conditions triggers
	STEP_COND[CURR_STEP].0 := CURR_MI_CODE.CHR_1 <> MI_CODE_CFG[4].CHR_1 OR
							  CURR_MI_CODE.CHR_2 <> MI_CODE_CFG[4].CHR_2;			// ALL MI CODE EXCEPT PASSWORD
	STEP_COND[CURR_STEP].1 := CURR_MI_CODE.CHR_1 = MI_CODE_CFG[4].CHR_1 AND
							  CURR_MI_CODE.CHR_2 = MI_CODE_CFG[4].CHR_2;			// PASSWORD MI CODE
//	STEP 1 TRANSITIONS	
//	Transitions triggers
	STEP_TRANS[CURR_STEP].0 := STEP_COND[CURR_STEP].0;
	STEP_TRANS[CURR_STEP].1 := STEP_COND[CURR_STEP].1; 	 
//	Transition ( DATA PROCESSING to CRC CALCULATION )	
	IF STEP_TRANS[CURR_STEP].0 THEN
		CURR_STEP := 2;																// SET VALUE
	END_IF;	
//	Transition ( DATA PROCESSING to PASSWORD BUILDER )	
	IF STEP_TRANS[CURR_STEP].1 THEN
		CURR_STEP := 3;																// SET VALUE
	END_IF;
	
END_IF;		

// STEP 2 ( CRC CALCULATION )
IF CURR_STEP = 2 THEN
	
//	STEP 2 FIRST SCAND CYCLE	
	IF CURR_STEP <> LAST_STEP THEN 
		STEP_TRANS[CURR_STEP] := 0;													// RESET VALUE
		STEP_COND[CURR_STEP] := 0;													// RESET VALUE
		LAST_STEP := CURR_STEP;														// SET VALUE
	END_IF;
	
//	STEP 2 CONTROL
//	CRC calculation
	CRC_CCITT := 16#1021;															// SET VALUE
	ACC := 0;																		// RESET VALUE
	DATA_LOCAL := 0;																// RESET VALUE
	FOR i := 0 TO PACKET_LOCAL.LEN-1 DO												// SET VALUES
		// Transfer data in from the traffic controller
		CRC_DATA[i] := PACKET_LOCAL.DATA[i];
		// Shift it 8 positions
		CRC_DATA_LOCAL := CRC_DATA[i] * 256; 
		// Loop to repeat acction for 8 first bits
		FOR x := 0 to 7 DO
			// (( Data XOR ACC ) AND 8000(HEX)) = 8000(HEX)
			IF ((CRC_DATA_LOCAL XOR ACC) AND 16#8000) = 16#8000  THEN
				// (Shift 1 position ACC) XOR CRC_CCITT
				ACC := (ACC * 2) XOR CRC_CCITT;
			 	else 
				// Shift 1 position ACC
				ACC := (ACC * 2);		
			END_IF;
			// Shift 1 position current data
			CRC_DATA_LOCAL := CRC_DATA_LOCAL *2;		
		END_FOR;	
		// Transfer the result to an array
		ACC := ACC; 
	END_FOR;	
	
//	STEP 2 CONDITIONS
//	Conditions triggers
	STEP_COND[CURR_STEP].0 := STEP_TRANS[1].0;										// REST OF PACKETS
	STEP_COND[CURR_STEP].1 := STEP_TRANS[1].1;										// PASSWORD PACKET
		
//	STEP 2 TRANSITIONS		
//	Transitions triggers
	STEP_TRANS[CURR_STEP].0 := STEP_COND[CURR_STEP].0 OR
							   STEP_COND[CURR_STEP].1;
//	Transition ( CRC CALCULATION to CONVERSION 4 ASCII )	
	IF STEP_TRANS[CURR_STEP].0 THEN
		CURR_STEP := 4;																// SET VALUE
	END_IF;
	
END_IF;

//	STEP 3 ( PASSWORD BUILDER )
IF CURR_STEP = 3 THEN
	
//	STEP 3 FIRST SCAND CYCLE	
	IF CURR_STEP <> LAST_STEP THEN 
		STEP_TRANS[CURR_STEP] := 0;													// RESET VALUE
		STEP_COND[CURR_STEP] := 0;													// RESET VALUE
		LAST_STEP := CURR_STEP;														// SET VALUE
	END_IF;
	
//	STEP 3 CONTROL
//	Password generator
	FOR i:= 0 TO 15 DO 																// SET VALUE
		IF ASCII_CHR_CFG[i] = PACKET_FDBK_MI.DATA[10] THEN
			SEED_CHR_1 := i;
			EXIT;
		END_IF;
	END_FOR;
	FOR i:= 0 TO 15 DO 																// SET VALUE
		IF ASCII_CHR_CFG[i] = PACKET_FDBK_MI.DATA[11] THEN
			SEED_CHR_2 := i;
			EXIT;
		END_IF;
	END_FOR;
	SHIFT_SEED_CHR_1 := SEED_CHR_1 * 16;											// SET VALUE
	JOIN_SEED := SEED_CHR_2 OR SHIFT_SEED_CHR_1;									// SET VALUE
	SEED_CONST := JOIN_SEED + PASS_SEED_CFG;										// SET VALUE
	SEED_LOCAL := SEED_CONST AND 255;												// SET VALUE
	FOR i := 1 to 16 DO																// SET VALUES 
		IF ((SEED_LOCAL AND 16#0000_0020) = 0) THEN	
			BIT_5 :=0;
		 	else 
			BIT_5 :=1;
		END_IF;
		IF ((SEED_LOCAL AND 16#0000_0080) = 0) THEN		
			BIT_7 :=0;
		 	else 
			BIT_7 :=1;
		END_IF;
		IF ((SEED_LOCAL AND 16#0000_0100) = 0) THEN		
			BIT_8 :=0;
		 	else 
			BIT_8 :=1;
		END_IF;
		IF (BIT_5 xor BIT_7 xor BIT_8) THEN		
			BITS_XOR :=1;
		 	else 
			BITS_XOR :=0;
		END_IF;
		SEED_LOCAL := SEED_LOCAL *2;	
		SEED_LOCAL := SEED_LOCAL + BITS_XOR;	
	END_FOR;
	SEED_LOCAL := SEED_LOCAL + PASS_OFFSET_CFG; 
	SEED_DEC := SEED_LOCAL;
	
//	STEP 3 CONDITIONS
//	Conditions triggers
	STEP_COND[CURR_STEP].0 := STEP_TRANS[1].1;										// PASSWORD PACKET
		
//	STEP 3 TRANSITIONS		
//	Transitions triggers
	STEP_TRANS[CURR_STEP].0 := STEP_COND[CURR_STEP].0;
//	Transition ( PASSWORD BUILDER to CONVERSION 4 ASCII )	
	IF STEP_TRANS[CURR_STEP].0 THEN
		CURR_STEP := 4;																// SET VALUE
	END_IF;

END_IF;

//	STEP 4 ( CONVERSION 4 ASCII )
IF CURR_STEP = 4 THEN
	
//	STEP 4 FIRST SCAND CYCLE	
	IF CURR_STEP <> LAST_STEP THEN 
		STEP_TRANS[CURR_STEP] := 0;													// RESET VALUE
		STEP_COND[CURR_STEP] := 0;													// RESET VALUE
		LAST_STEP := CURR_STEP;														// SET VALUE
	END_IF;	
	
//	STEP 4 CONTROL
//	CRC calculation selection
	IF STEP_TRANS[1].0 OR STEP_COND[2].1 THEN
		DATA_LOCAL := ACC;															// SET VALUE
	END_IF;
//	Password generator selection
	IF CURR_MI_CODE.CHR_1 = MI_CODE_CFG[4].CHR_1 AND
	   CURR_MI_CODE.CHR_2 = MI_CODE_CFG[4].CHR_2 AND
	   NOT STEP_COND[2].1 THEN
		DATA_LOCAL := SEED_LOCAL;													// SET VALUE
	END_IF;	
//	Operation 1 ( fourth password byte ) 
	MATH_OPER[0] := DATA_LOCAL AND 15;
	MATH_OPER[0] := MATH_OPER[0] + 48;
	IF MATH_OPER[0] > 57 THEN
		MATH_OPER[0] := MATH_OPER[0] + 7;
	END_IF;
//	Operation 2 ( third password byte )
	MATH_OPER[1] := DATA_LOCAL AND 240;
	MATH_OPER[1] := MATH_OPER[1] / 16;
	MATH_OPER[1] := MATH_OPER[1] + 48;
	IF MATH_OPER[1] > 57 THEN
		MATH_OPER[1] := MATH_OPER[1] + 7;
	END_IF;
//	Operation 3 ( second password byte )
	MATH_OPER[2] := DATA_LOCAL AND 3840;
	MATH_OPER[2] := MATH_OPER[2] / 256;
	MATH_OPER[2] := MATH_OPER[2] + 48;
	IF MATH_OPER[2] > 57 THEN
		MATH_OPER[2] := MATH_OPER[2] + 7;
	END_IF;
//	Operation 4 ( first password byte )
	MATH_OPER[3] := DATA_LOCAL AND 61440;
	MATH_OPER[3] := MATH_OPER[3] / 4096;
	MATH_OPER[3] := MATH_OPER[3] + 48;
	IF MATH_OPER[3] > 57 THEN
		MATH_OPER[3] := MATH_OPER[3] + 7;
	END_IF;	
//	Transfer values
	IF STEP_TRANS[1].0 OR STEP_COND[2].1 THEN
		PACKET_LOCAL.DATA[PACKET_LOCAL.LEN] := MATH_OPER[3];
		PACKET_LOCAL.DATA[PACKET_LOCAL.LEN+1] := MATH_OPER[2];
		PACKET_LOCAL.DATA[PACKET_LOCAL.LEN+2] := MATH_OPER[1];
		PACKET_LOCAL.DATA[PACKET_LOCAL.LEN+3] := MATH_OPER[0];
		PACKET_LOCAL.DATA[PACKET_LOCAL.LEN+4] := 3;
		PACKET_LOCAL.LEN := PACKET_LOCAL.LEN + 5;
	ELSIF CURR_MI_CODE.CHR_1 = MI_CODE_CFG[4].CHR_1 AND
	      CURR_MI_CODE.CHR_2 = MI_CODE_CFG[4].CHR_2 AND
		  NOT STEP_COND[2].1 THEN
		PACKET_LOCAL.DATA[PACKET_LOCAL.LEN] := MATH_OPER[3];
		PACKET_LOCAL.DATA[PACKET_LOCAL.LEN+1] := MATH_OPER[2];
		PACKET_LOCAL.DATA[PACKET_LOCAL.LEN+2] := MATH_OPER[1];
		PACKET_LOCAL.DATA[PACKET_LOCAL.LEN+3] := MATH_OPER[0];
		PACKET_LOCAL.LEN := PACKET_LOCAL.LEN + 4;
		PACKET_LOCAL.DATA[2] := PASS_1_CYCLE_CFG;									// SET VALUE
		PACKET_LOCAL.DATA[4] := PASS_1_CYCLE_CFG;									// SET VALUE
	END_IF;
	
//	STEP 4 CONDITIONS
//	Conditions triggers
	STEP_COND[CURR_STEP].0 := STEP_TRANS[1].0;										// PACKET READY TO SEND
	STEP_COND[CURR_STEP].1 := CURR_MI_CODE.CHR_1 = MI_CODE_CFG[4].CHR_1 AND
	   						  CURR_MI_CODE.CHR_2 = MI_CODE_CFG[4].CHR_2;			// PASSWORD BUILT
	STEP_COND[CURR_STEP].2 := NOT STEP_COND[2].1;									// PASSWORD CRC NOT BUILD
	STEP_COND[CURR_STEP].3 := STEP_COND[2].1;										// PASSWORD CRC BUILT
		
//	STEP 4 TRANSITIONS		
//	Transitions triggers
	STEP_TRANS[CURR_STEP].0 := STEP_COND[CURR_STEP].0 OR
							   (STEP_COND[CURR_STEP].1 AND
							   STEP_COND[CURR_STEP].3);
	STEP_TRANS[CURR_STEP].1 := STEP_COND[CURR_STEP].1 AND 
							   STEP_COND[CURR_STEP].2;
//	Transition ( CONVERSION 4 ASCII to SEND DATA PACKET )	
	IF STEP_TRANS[CURR_STEP].0 THEN
		CURR_STEP := 5;																// SET VALUE
	END_IF;
//	Transition ( CONVERSION 4 ASCII to CRC CALCULATION )	
	IF STEP_TRANS[CURR_STEP].1 THEN
		CURR_STEP := 2;																// SET VALUE
	END_IF;

END_IF;

// STEP 5 ( SEND DATA PACKET )
IF CURR_STEP = 5 THEN
	
//	STEP 5 FIRST SCAND CYCLE			
	IF CURR_STEP <> LAST_STEP THEN 
		STEP_COND[CURR_STEP] := 0;													// RESET VALUE
		STEP_TRANS[CURR_STEP] := 0;													// RESET VALUE
		LAST_STEP := CURR_STEP;														// SET VALUE
		BUFFER_ONS := 0;
	END_IF;
	
//	STEP 5 CONTROL
//	Copy local packet to command packet 	
	PACKET_CMD_MI.TYPE := TRANS_TYPE_LOCAL;
	COP(PACKET_LOCAL,PACKET_CMD_MI.CMD,PACKET_LOCAL.LEN);
//	Command buffer recorded one-shoot
	IF NOT BUFFER_ONS THEN
		COP(PACKET_LOCAL,BUFFER_CMD[BUFFER_INDEX],PACKET_LOCAL.LEN);
		BUFFER_INDEX := BUFFER_INDEX + 1;
		IF BUFFER_INDEX > 49 THEN 
			BUFFER_INDEX := 0; 
		END_IF;
		BUFFER_ONS := 1;
	END_IF;
	
//	STEP 5 CONDITIONS
//	Conditions triggers
	STEP_COND[CURR_STEP].0 := PACKET_CMD_MI.CMD.LEN <> 0 AND BUFFER_INDEX < 256;								// PACKET COPIED
	
//	STEP 5 TRANSITIONS		
//	Transitions triggers
	STEP_TRANS[CURR_STEP].0 := STEP_COND[CURR_STEP].0; 	
//	Transition ( SEND DATA PACKET to RECEIVED DATA PACKET )	
	IF STEP_TRANS[CURR_STEP].0 THEN
		CURR_STEP := 6;																// SET VALUE
	END_IF;
	
END_IF;	

// STEP 6 ( RECEIVED DATA PACKET )
IF CURR_STEP = 6 THEN
	
//	STEP 6 FIRST SCAND CYCLE		
	IF CURR_STEP <> LAST_STEP THEN 
		STEP_COND[CURR_STEP] := 0;													// RESET VALUE
		STEP_TRANS[CURR_STEP] := 0;													// RESET VALUE
		LAST_STEP := CURR_STEP;														// SET VALUE
		WRITE_ONS := 0;																// RESET VALUE
		VER_PACKET_STS := 0;														// RESET VALUE
		FOR i := 0 TO VER_PACKET.LEN DO 
			VER_PACKET.DATA[i] := 0;
		END_FOR;
		VER_PACKET.LEN := 0;
	END_IF;
	
//	STEP 6 CONTROL
//	System does not expect a answer
	IF TRANS_TYPE_LOCAL = 1 AND NOT WRITE_ONS THEN
//		Cycle pisition
		IF CYCLE_POS_CHR_1 = 70 AND CYCLE_POS_CHR_2 = 70 THEN 
			CYCLE_POS_CHR_1 := 48;													// SET VALUE
			CYCLE_POS_CHR_2 := 48;													// SET VALUE
		ELSE	
			FOR x := 0 TO 15 DO 																	// SET VALUE
				IF ASCII_CHR_CFG[x] = CYCLE_POS_CHR_1 THEN
					CYCLE_POS_CHR_LOCAL := x * 16;		
					EXIT;
				END_IF;
			END_FOR;
			FOR x:= 0 TO 15 DO 																		// SET VALUE
				IF ASCII_CHR_CFG[x] = CYCLE_POS_CHR_2 THEN
					CYCLE_POS_CHR_LOCAL := CYCLE_POS_CHR_LOCAL + x;		
					EXIT;
				END_IF;
			END_FOR;
			CYCLE_POS_CHR_LOCAL := CYCLE_POS_CHR_LOCAL + 1;
			DEC_1_CHR := CYCLE_POS_CHR_LOCAL AND 15;												// SET VALUE
			DEC_2_CHR := (CYCLE_POS_CHR_LOCAL AND 240)/16;														// SET VALUE
			FOR i:= 0 TO 15 DO 																		// SET VALUE
				IF i = DEC_1_CHR THEN
					CYCLE_POS_CHR_2 := ASCII_CHR_CFG[i];
					EXIT;
				END_IF;
			END_FOR;	
			FOR i:= 0 TO 15 DO																		// SET VALUE
				IF i = DEC_2_CHR THEN
					CYCLE_POS_CHR_1 := ASCII_CHR_CFG[i];
					EXIT;
				END_IF;
			END_FOR;			
		END_IF;	
		WRITE_ONS := 1;	
	END_IF;
//	Verify transaction coming from traffic controller
	IF PACKET_FDBK_MI <> 'NONE' AND PACKET_FDBK_MI <> '$0F' AND 
	   PACKET_FDBK_MI <> 'WRONG MSG' AND PACKET_FDBK_MI.LEN > 4 AND
	   TRANS_TYPE_LOCAL = 2 THEN  
//		Get data without ending and CRC
		FOR i := PACKET_FDBK_MI.LEN - 6 TO 0 BY -1 DO 
			VER_PACKET.DATA[i] := PACKET_FDBK_MI.DATA[i];
		END_FOR;
		VER_PACKET.LEN := PACKET_FDBK_MI.LEN - 6;
//		CRC calculation
		CRC_CCITT := 16#1021;															// SET VALUE
		ACC := 0;																		// RESET VALUE
		DATA_LOCAL := 0;																// RESET VALUE
		FOR i := 0 TO VER_PACKET.LEN DO												// SET VALUES
			// Transfer data in from the traffic controller
			CRC_DATA[i] := VER_PACKET.DATA[i];
			// Shift it 8 positions
			CRC_DATA_LOCAL := CRC_DATA[i] * 256; 
			// Loop to repeat acction for 8 first bits
			FOR x := 0 to 7 DO
				// (( Data XOR ACC ) AND 8000(HEX)) = 8000(HEX)
				IF ((CRC_DATA_LOCAL XOR ACC) AND 16#8000) = 16#8000  THEN
					// (Shift 1 position ACC) XOR CRC_CCITT
					ACC := (ACC * 2) XOR CRC_CCITT;
				 	else 
					// Shift 1 position ACC
					ACC := (ACC * 2);		
				END_IF;
				// Shift 1 position current data
				CRC_DATA_LOCAL := CRC_DATA_LOCAL *2;		
			END_FOR;	
			// Transfer the result to an array
			ACC := ACC; 
		END_FOR;
//		Transfer result		
		DATA_LOCAL := ACC;
//		Operation 1 ( fourth password byte ) 
		MATH_OPER[0] := DATA_LOCAL AND 15;
		MATH_OPER[0] := MATH_OPER[0] + 48;
		IF MATH_OPER[0] > 57 THEN
			MATH_OPER[0] := MATH_OPER[0] + 7;
		END_IF;
//		Operation 2 ( third password byte )
		MATH_OPER[1] := DATA_LOCAL AND 240;
		MATH_OPER[1] := MATH_OPER[1] / 16;
		MATH_OPER[1] := MATH_OPER[1] + 48;
		IF MATH_OPER[1] > 57 THEN
			MATH_OPER[1] := MATH_OPER[1] + 7;
		END_IF;
//		Operation 3 ( second password byte )
		MATH_OPER[2] := DATA_LOCAL AND 3840;
		MATH_OPER[2] := MATH_OPER[2] / 256;
		MATH_OPER[2] := MATH_OPER[2] + 48;
		IF MATH_OPER[2] > 57 THEN
			MATH_OPER[2] := MATH_OPER[2] + 7;
		END_IF;
//		Operation 4 ( first password byte )
		MATH_OPER[3] := DATA_LOCAL AND 61440;
		MATH_OPER[3] := MATH_OPER[3] / 4096;
		MATH_OPER[3] := MATH_OPER[3] + 48;
		IF MATH_OPER[3] > 57 THEN
			MATH_OPER[3] := MATH_OPER[3] + 7;
		END_IF;	
//		Check CRC match
		IF PACKET_FDBK_MI.DATA[PACKET_FDBK_MI.LEN-5] = MATH_OPER[3] AND
		   PACKET_FDBK_MI.DATA[PACKET_FDBK_MI.LEN-4] = MATH_OPER[2] AND
		   PACKET_FDBK_MI.DATA[PACKET_FDBK_MI.LEN-3] = MATH_OPER[1] AND
		   PACKET_FDBK_MI.DATA[PACKET_FDBK_MI.LEN-2] = MATH_OPER[0] THEN 
			VER_PACKET_STS := 1;														// SET VALUE
		END_IF;	
	ELSIF (PACKET_FDBK_MI <> 'NONE' OR PACKET_FDBK_MI <> '$0F' OR 
	       PACKET_FDBK_MI <> 'WRONG MSG')  AND TRANS_TYPE_LOCAL = 2  THEN 	
			VER_PACKET_STS := 1;														// SET VALUE	   
	END_IF;

//	STEP 6 CONDITIONS
//	Conditions triggers
	STEP_COND[CURR_STEP].0 := PACKET_CMD_MI.CMD.LEN = 0;							// PACKET SENT
	STEP_COND[CURR_STEP].1 := PACKET_FDBK_MI.LEN <> 0;								// PACKET ANSWER
	STEP_COND[CURR_STEP].2 := TRANS_TYPE_LOCAL = 1;									// TRANSITION TO DATA ANALYSES
	STEP_COND[CURR_STEP].3 := TRANS_TYPE_LOCAL = 2;									// TRANSITION TO DATA EXTRACTION
	STEP_COND[CURR_STEP].4 := VER_PACKET_STS;										// TRANSACTION ACCEPTED
	STEP_COND[CURR_STEP].5 := NOT VER_PACKET_STS;									// TRANSACTION REJECTED
		
//	STEP 6 TRANSITIONS		
//	Transitions triggers
	STEP_TRANS[CURR_STEP].0 := (STEP_COND[CURR_STEP].0 AND
							   STEP_COND[CURR_STEP].2) OR
							   (STEP_COND[CURR_STEP].1 AND
							   STEP_COND[CURR_STEP].5); 	
	STEP_TRANS[CURR_STEP].1 := STEP_COND[CURR_STEP].1 AND
							   STEP_COND[CURR_STEP].3 AND
							   STEP_COND[CURR_STEP].4; 
//	Transition ( RECEIVED DATA PACKET to DATA ANALYSES )	
	IF STEP_TRANS[CURR_STEP].0 THEN
		CURR_STEP := 0;																// SET VALUE
	END_IF;
//	Transition ( RECEIVED DATA PACKET to DATA EXTRACTION )	
	IF STEP_TRANS[CURR_STEP].1 THEN
		CURR_STEP := 7;																// SET VALUE
	END_IF;

END_IF;	

// STEP 7 ( DATA EXTRACTION )
IF CURR_STEP = 7 THEN
	
//	STEP 7 FIRST SCAND CYCLE	
	IF CURR_STEP <> LAST_STEP THEN 
		STEP_COND[CURR_STEP] := 0;													// RESET VALUE
		STEP_TRANS[CURR_STEP] := 0;													// RESET VALUE
		LAST_STEP := CURR_STEP;														// SET VALUE
		FILTER_ONS := 1;
		DEVICES_CTRL_MI.APP_REQ := 0;
		BUFFER_ONS := 0;
	END_IF;
	
//	STEP 7 CONTROL
//	Feedback buffer 
	IF NOT BUFFER_ONS THEN
		COP(PACKET_FDBK_MI,BUFFER_FDBK[BUFFER_INDEX],PACKET_FDBK_MI.LEN);
		BUFFER_ONS := 1;
	END_IF;	
//	Wrong packet
	IF PACKET_FDBK_MI = 'WRONG MSG' OR PACKET_FDBK_MI = 'NONE' THEN 
		DEVICE_STS.5 := 0;
	END_IF;
//	Packet rejected
	IF PACKET_FDBK_MI.DATA[0] = 15 THEN
		DEVICES_CTRL_MI.APP_REQ := 7;												// SET VALUE	
	END_IF;
//	Unacknowledged application message 
	IF PACKET_FDBK_MI.DATA[8] = 48 AND 
	   PACKET_FDBK_MI.DATA[9] = 48 THEN
//		Application error number
		FOR x := 0 TO 15 DO 														// SET VALUE
			IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[10] THEN
				APP_ER_NUM_ENG := x * 16;		
				EXIT;
			END_IF;
		END_FOR;
		FOR x:= 0 TO 15 DO 															// SET VALUE
			IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[11] THEN
				APP_ER_NUM_ENG := APP_ER_NUM_ENG + x;		
				EXIT;
			END_IF;
		END_FOR;
		DEVICES_CTRL_MI.APP_REQ := 7;												// SET VALUE	
	END_IF;	
//	Acknowledged application message 
	IF PACKET_FDBK_MI.DATA[8] = MI_CODE_CFG[1].CHR_1 AND 
	   PACKET_FDBK_MI.DATA[9] = MI_CODE_CFG[1].CHR_2 THEN
//		Password application message
		IF PACKET_FDBK_MI.DATA[10] = MI_CODE_CFG[4].CHR_1 AND 
		   PACKET_FDBK_MI.DATA[11] = MI_CODE_CFG[4].CHR_2 THEN
			DEVICE_STS.5 := 1;															// SET VALUE
			DEVICES_CTRL_MI.APP_REQ := 7;												// SET VALUE	
		END_IF;
//		End session application message
		IF PACKET_FDBK_MI.DATA[10] = MI_CODE_CFG[7].CHR_1 AND 
		   PACKET_FDBK_MI.DATA[11] = MI_CODE_CFG[7].CHR_2 THEN 
			DEVICE_STS.5 := 0;															// SET VALUE
			DEVICES_CTRL_MI.APP_REQ := 0;												// SET VALUE
		END_IF;
//		Sign display frame application message 
		IF PACKET_FDBK_MI.DATA[10] = MI_CODE_CFG[15].CHR_1 AND 
		   PACKET_FDBK_MI.DATA[11] = MI_CODE_CFG[15].CHR_2 THEN 
			DEVICES_MON_MI[0].FRAME_ID:= DEVICE_MI_LOCAL.DEVICES[0].CMD;
			DEVICES_CTRL_MI.APP_REQ := 0;												// SET VALUE
		END_IF;
	END_IF;
//	Sign status reply application message  
	IF PACKET_FDBK_MI.DATA[8] = MI_CODE_CFG[6].CHR_1 AND 
	   PACKET_FDBK_MI.DATA[9] = MI_CODE_CFG[6].CHR_2 THEN			
//		Application error number
		FOR x := 0 TO 15 DO 														// SET VALUE
			IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[12] THEN
				APP_ER_NUM_ENG := x * 16;		
				EXIT;
			END_IF;
		END_FOR;
		FOR x:= 0 TO 15 DO 															// SET VALUE
			IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[13] THEN
				APP_ER_NUM_ENG := APP_ER_NUM_ENG + x;		
				EXIT;
			END_IF;
		END_FOR;
//		Reset data request message configuration
		FOR i := 0 TO 5 DO 
			MSG_CFG_LOCAL[i] := 0;
			MSG_STORED_MI[i].MSG_ID := 0;
			MSG_STORED_MI[i].TIME_BTW_FRAMES := 0;
			FOR x := 0 TO 5 DO 
				MSG_STORED_MI[i].FRAME_ID[x] := 0;
				MSG_STORED_MI[i].DISPLAY_TIME[x] := 0;
			END_FOR;
		END_FOR;
//		Number of signs attached to the sign controller	
		FOR x := 0 TO 15 DO 														// SET VALUE
			IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[34] THEN
				DEVICE_MI_LOCAL.NUM_SIGNS_CONTROLLER := x * 16;		
				EXIT;
			END_IF;
		END_FOR;
		FOR x:= 0 TO 15 DO 															// SET VALUE
			IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[35] THEN
				DEVICE_MI_LOCAL.NUM_SIGNS_CONTROLLER := DEVICE_MI_LOCAL.NUM_SIGNS_CONTROLLER + x;		
				EXIT;
			END_IF;
		END_FOR;
//		Device information			
		z := 36;																	// SET VALUE
		FOR i := 0 TO DEVICE_MI_LOCAL.NUM_SIGNS_CONTROLLER -1 DO
//			Device ID
			FOR x := 0 TO 15 DO 													// SET VALUE
				IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[z] THEN
					DEVICES_MON_MI[i].FIELD_SIGN_ID := x * 16;		
					EXIT;
				END_IF;
			END_FOR;
			FOR x:= 0 TO 15 DO 														// SET VALUE
				IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[z+1] THEN
					DEVICES_MON_MI[i].FIELD_SIGN_ID := DEVICES_MON_MI[i].FIELD_SIGN_ID + x;	
					EXIT;
				END_IF;
			END_FOR;
//			Device status ( 0 : Disable / 1 : Enable )
			FOR x := 0 TO 15 DO 													// SET VALUE
				IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[z+4] THEN
					DEVICES_MON_MI[i].SIGN_AVAIL_STS := x * 16;		
					EXIT;
				END_IF;
			END_FOR;
			FOR x:= 0 TO 15 DO 														// SET VALUE
				IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[z+5] THEN
					DEVICES_MON_MI[i].SIGN_AVAIL_STS := DEVICES_MON_MI[i].SIGN_AVAIL_STS + x;	
					EXIT;
				END_IF;
			END_FOR;
//			Frame ID displayed
			FOR x := 0 TO 15 DO 													// SET VALUE
				IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[z+6] THEN
					DEVICES_MON_MI[i].FRAME_ID := x * 16;		
					EXIT;
				END_IF;
			END_FOR;
			FOR x:= 0 TO 15 DO 														// SET VALUE
				IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[z+7] THEN
					DEVICES_MON_MI[i].FRAME_ID := DEVICES_MON_MI[i].FRAME_ID + x;	
					EXIT;
				END_IF;
			END_FOR;	
////			Message ID displayed
//			FOR x := 0 TO 15 DO 													// SET VALUE
//				IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[z+10] THEN
//					DEVICES_MON_MI[i].MSG_ID := x * 16;		
//					EXIT;
//				END_IF;
//			END_FOR;
//			FOR x:= 0 TO 15 DO 														// SET VALUE
//				IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[z+11] THEN
//					DEVICES_MON_MI[i].MSG_ID := DEVICES_MON_MI[i].MSG_ID + x;	
//					EXIT;
//				END_IF;
//			END_FOR;
			DEVICES_MON_MI[i].MSG_ID := 0;	// JUST FOR TEST
//			Request Message configuration
			IF DEVICES_MON_MI[i].MSG_ID <> 0 THEN 
				FOR x := 0 TO 5 DO
//					Message ID already recorded
					IF MSG_CFG_LOCAL[x] = DEVICES_MON_MI[i].MSG_ID THEN
						EXIT;
//					Record the message
					ELSIF MSG_CFG_LOCAL[x] = 0 THEN 
						DEVICES_CTRL_MI.APP_REQ := 10;
						MSG_CFG_LOCAL[x] := DEVICES_MON_MI[i].MSG_ID;
						EXIT;
					END_IF;
				END_FOR;
			END_IF;
			z := z + 18;				
		END_FOR;
// JUST FOR TEST
////		Reset command type after has been process	
//		IF DEVICES_CTRL_MI.APP_REQ <> 10 THEN 
//			DEVICES_CTRL_MI.APP_REQ := 3;													// SET VALUE
//		END_IF;
// JUST FOR TEST
	END_IF;
//	Sing Request Stored Message application	
	IF PACKET_FDBK_MI.DATA[8] = MI_CODE_CFG[14].CHR_1 AND 
	   PACKET_FDBK_MI.DATA[9] = MI_CODE_CFG[14].CHR_2 THEN
//		Number of page displayed		
		CUR_NUM_FRAME := 1;																	// SET 1 PAGE
		IF PACKET_FDBK_MI.DATA[30] = 3 AND PACKET_FDBK_MI.DATA[44] <> 3 THEN	
			CUR_NUM_FRAME := 2;																// SET 2 PAGE
		ELSIF PACKET_FDBK_MI.DATA[34] = 3 AND PACKET_FDBK_MI.DATA[44] <> 3 THEN	
			CUR_NUM_FRAME := 3;																// SET 3 PAGE
		ELSIF PACKET_FDBK_MI.DATA[38] = 3 AND PACKET_FDBK_MI.DATA[44] <> 3 THEN	
			CUR_NUM_FRAME := 4;																// SET 4 PAGE
		ELSIF PACKET_FDBK_MI.DATA[42] = 3 AND PACKET_FDBK_MI.DATA[44] <> 3 THEN	
			CUR_NUM_FRAME := 5;																// SET 5 PAGE
		ELSIF PACKET_FDBK_MI.DATA[44] = 3 THEN	
			CUR_NUM_FRAME := 6;																// SET 6 PAGE	
		END_IF;
//		Message ID
		FOR x := 0 TO 15 DO 																// SET VALUE
			IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[10] THEN
				MSG_STORED_MI[CUR_MSG_INDEX_POS].MSG_ID := x * 16;		
				EXIT;
			END_IF;
		END_FOR;
		FOR x:= 0 TO 15 DO 																	// SET VALUE
			IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[11] THEN
				MSG_STORED_MI[CUR_MSG_INDEX_POS].MSG_ID := MSG_STORED_MI[CUR_MSG_INDEX_POS].MSG_ID + x;	
				EXIT;
			END_IF;
		END_FOR; 
//		Transition time between frames
		FOR x := 0 TO 15 DO 																// SET VALUE
			IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[14] THEN
				MSG_STORED_MI[CUR_MSG_INDEX_POS].TIME_BTW_FRAMES := x * 16;		
				EXIT;
			END_IF;
		END_FOR;
		FOR x:= 0 TO 15 DO 																	// SET VALUE
			IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[15] THEN
				MSG_STORED_MI[CUR_MSG_INDEX_POS].TIME_BTW_FRAMES := MSG_STORED_MI[CUR_MSG_INDEX_POS].TIME_BTW_FRAMES + x;	
				EXIT;
			END_IF;
		END_FOR;  
//		Frame information base on number of page	
		z := 16;
		FOR i := 0 TO CUR_NUM_FRAME - 1 DO 
//			Frame x ID
			FOR x := 0 TO 15 DO 																// SET VALUE
				IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[z] THEN
					MSG_STORED_MI[CUR_MSG_INDEX_POS].FRAME_ID[i] := x * 16;		
					EXIT;
				END_IF;
			END_FOR;
			FOR x:= 0 TO 15 DO 																	// SET VALUE
				IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[z + 1] THEN
					MSG_STORED_MI[CUR_MSG_INDEX_POS].FRAME_ID[i] := MSG_STORED_MI[CUR_MSG_INDEX_POS].FRAME_ID[i] + x;	
					EXIT;
				END_IF;
			END_FOR;  
//			Frame x time on
			FOR x := 0 TO 15 DO 																// SET VALUE
				IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[z + 2] THEN
					MSG_STORED_MI[CUR_MSG_INDEX_POS].DISPLAY_TIME[i] := x * 16;		
					EXIT;
				END_IF;
			END_FOR;
			FOR x:= 0 TO 15 DO 																	// SET VALUE
				IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[z + 3] THEN
					MSG_STORED_MI[CUR_MSG_INDEX_POS].DISPLAY_TIME[i] := MSG_STORED_MI[CUR_MSG_INDEX_POS].DISPLAY_TIME[i] + x;	
					EXIT;
				END_IF;
			END_FOR; 
			z := z + 4;
		END_FOR;
//		Check if there are more message
		IF MSG_CFG_LOCAL[0] = 0 THEN  
			DEVICES_CTRL_MI.APP_REQ := 3;													// SET VALUE
		ELSE 
			
			DEVICES_CTRL_MI.APP_REQ := 10;													// SET VALUE
		END_IF;
	END_IF;
//	Fault Log Reply
	IF PACKET_FDBK_MI.DATA[8] = MI_CODE_CFG[21].CHR_1 AND 
	   PACKET_FDBK_MI.DATA[9] = MI_CODE_CFG[21].CHR_2 THEN 
//		Number of entries to follow (0-20)
		FOR x := 0 TO 15 DO 														// SET VALUE
			IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[10] THEN
				EV_REG_IN.NUM := x * 16;		
				EXIT;
			END_IF;
		END_FOR;
		FOR x:= 0 TO 15 DO 															// SET VALUE
			IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[11] THEN
				EV_REG_IN.NUM := EV_REG_IN.NUM + x;		
				EXIT;
			END_IF;
		END_FOR;	
//		Setting number of logs in the register for the loop
		IF EV_REG_IN.NUM > 0 AND EV_REG_IN.NUM <= 20 THEN
			NUM_REG := EV_REG_IN.NUM - 1;
//			Feedback data structure ( index start position )
			z := 12;
			FOR i:= 0 TO NUM_REG DO
//				Event type ( 0 : Controller / <> 0 : Sign )
				FOR x := 0 TO 15 DO 													// SET VALUE
					IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[z] THEN
						EV_REG_IN.REG[i].TYPE := x * 16;		
						EXIT;
					END_IF;
				END_FOR;
				FOR x:= 0 TO 15 DO 														// SET VALUE
					IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[z+1] THEN
						EV_REG_IN.REG[i].TYPE := EV_REG_IN.REG[i].TYPE + x;	
						EXIT;
					END_IF;
				END_FOR;
//				Error code	
				FOR x := 0 TO 15 DO 													// SET VALUE
					IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[z+18] THEN
						EV_REG_IN.REG[i].ER_CODE := x * 16;		
						EXIT;
					END_IF;
				END_FOR;
				FOR x:= 0 TO 15 DO 														// SET VALUE
					IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[z+19] THEN
						EV_REG_IN.REG[i].ER_CODE := EV_REG_IN.REG[i].ER_CODE + x;	
						EXIT;
					END_IF;
				END_FOR;		   
//				Event status ( 0 : Creared / 1 : Onset ) 	
				FOR x := 0 TO 15 DO 													// SET VALUE
					IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[z+20] THEN
						EV_REG_IN.REG[i].STS := x * 16;		
						EXIT;
					END_IF;
				END_FOR;
				FOR x:= 0 TO 15 DO 														// SET VALUE
					IF ASCII_CHR_CFG[x] = PACKET_FDBK_MI.DATA[z+21] THEN
						EV_REG_IN.REG[i].STS := EV_REG_IN.REG[i].STS + x;	
						EXIT;
					END_IF;
				END_FOR;
				z := z + 22;
			END_FOR;
////			Reset fault log 			
//			RST_LOG_MEM := 1;
		ELSE
			NUM_REG := 0;
		END_IF;
		IF FILTER_ONS THEN
//			SCADA events status
			FOR i := 0 TO NUM_REG DO
//				Controller events
				IF EV_REG_IN.REG[i].TYPE = 0 THEN 
//					Event selection					
					FOR z := 0 TO 39 DO
						IF EV_REG_IN.REG[i].ER_CODE = CTRLLR_EV_LOCAL[z].ER_CODE THEN 
							CTRLLR_EV_LOCAL[z].TYPE := EV_REG_IN.REG[i].TYPE;
//							Record event status
							IF EV_REG_IN.REG[i].STS = 0 THEN 
								CTRLLR_EV_LOCAL[z].CLEAR_STS := 1;
							ELSIF EV_REG_IN.REG[i].STS = 1 THEN  
								CTRLLR_EV_LOCAL[z].ONSET_STS := 1;
							END_IF;
//							Reset event status 
							IF CTRLLR_EV_LOCAL[z].CLEAR_STS = 1 AND CTRLLR_EV_LOCAL[z].ONSET_STS = 1 THEN 
								CTRLLR_EV_LOCAL[z].CLEAR_STS := 0;
								CTRLLR_EV_LOCAL[z].ONSET_STS := 0;
							END_IF;
							EXIT;
						END_IF;
					END_FOR;
//				Devices events
				ELSIF EV_REG_IN.REG[i].TYPE > 0 AND EV_REG_IN.REG[i].TYPE <= 12 THEN 
//					Device selected
					CUR_DEVICE_EV_SEL := EV_REG_IN.REG[i].TYPE - 1;
//					Event selection		
					FOR z := 0 TO 39 DO
						IF EV_REG_IN.REG[i].ER_CODE = DEVICE_EV_LOCAL[CUR_DEVICE_EV_SEL].DEVICE_EV[z].ER_CODE THEN 
							DEVICE_EV_LOCAL[CUR_DEVICE_EV_SEL].DEVICE_EV[z].TYPE := EV_REG_IN.REG[i].TYPE;
//							Record event status
							IF EV_REG_IN.REG[i].STS = 0 THEN 
								DEVICE_EV_LOCAL[CUR_DEVICE_EV_SEL].DEVICE_EV[z].CLEAR_STS := 1;
							ELSIF EV_REG_IN.REG[i].STS = 1 THEN  
								DEVICE_EV_LOCAL[CUR_DEVICE_EV_SEL].DEVICE_EV[z].ONSET_STS := 1;
							END_IF;
//							Reset event status 
							IF DEVICE_EV_LOCAL[CUR_DEVICE_EV_SEL].DEVICE_EV[z].CLEAR_STS = 1 AND DEVICE_EV_LOCAL[CUR_DEVICE_EV_SEL].DEVICE_EV[z].ONSET_STS = 1 THEN 
								DEVICE_EV_LOCAL[CUR_DEVICE_EV_SEL].DEVICE_EV[z].CLEAR_STS := 0;
								DEVICE_EV_LOCAL[CUR_DEVICE_EV_SEL].DEVICE_EV[z].ONSET_STS := 0;
							END_IF;
							EXIT;
						END_IF;
					END_FOR;
				END_IF;
			END_FOR;
//			Trigger controller events
			CTRLLR_ER_CODE_1_ENG.0 := CTRLLR_EV_LOCAL[0].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[0].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.1 := CTRLLR_EV_LOCAL[1].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[1].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.2 := CTRLLR_EV_LOCAL[2].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[2].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.3 := CTRLLR_EV_LOCAL[3].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[3].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.4 := CTRLLR_EV_LOCAL[4].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[4].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.5 := CTRLLR_EV_LOCAL[5].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[5].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.6 := CTRLLR_EV_LOCAL[6].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[6].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.7 := CTRLLR_EV_LOCAL[7].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[7].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.8 := CTRLLR_EV_LOCAL[8].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[8].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.9 := CTRLLR_EV_LOCAL[9].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[9].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.10 := CTRLLR_EV_LOCAL[10].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[10].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.11 := CTRLLR_EV_LOCAL[11].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[11].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.12 := CTRLLR_EV_LOCAL[12].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[12].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.13 := CTRLLR_EV_LOCAL[13].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[13].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.14 := CTRLLR_EV_LOCAL[14].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[14].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.15 := CTRLLR_EV_LOCAL[15].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[15].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.16 := CTRLLR_EV_LOCAL[16].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[16].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.17 := CTRLLR_EV_LOCAL[17].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[17].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.18 := CTRLLR_EV_LOCAL[18].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[18].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.19 := CTRLLR_EV_LOCAL[19].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[19].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.20 := CTRLLR_EV_LOCAL[20].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[20].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.21 := CTRLLR_EV_LOCAL[21].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[21].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.22 := CTRLLR_EV_LOCAL[22].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[22].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.23 := CTRLLR_EV_LOCAL[23].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[23].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.24 := CTRLLR_EV_LOCAL[24].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[24].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.25 := CTRLLR_EV_LOCAL[25].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[25].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.26 := CTRLLR_EV_LOCAL[26].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[26].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.27 := CTRLLR_EV_LOCAL[27].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[27].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.28 := CTRLLR_EV_LOCAL[28].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[28].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.29 := CTRLLR_EV_LOCAL[29].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[29].CLEAR_STS = 0;
			CTRLLR_ER_CODE_1_ENG.30 := CTRLLR_EV_LOCAL[30].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[30].CLEAR_STS = 0;
			CTRLLR_ER_CODE_2_ENG.0 := CTRLLR_EV_LOCAL[31].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[31].CLEAR_STS = 0;
			CTRLLR_ER_CODE_2_ENG.1 := CTRLLR_EV_LOCAL[32].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[32].CLEAR_STS = 0;
			CTRLLR_ER_CODE_2_ENG.2 := CTRLLR_EV_LOCAL[33].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[33].CLEAR_STS = 0;
			CTRLLR_ER_CODE_2_ENG.3 := CTRLLR_EV_LOCAL[34].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[34].CLEAR_STS = 0;
			CTRLLR_ER_CODE_2_ENG.4 := CTRLLR_EV_LOCAL[35].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[35].CLEAR_STS = 0;
			CTRLLR_ER_CODE_2_ENG.5 := CTRLLR_EV_LOCAL[36].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[36].CLEAR_STS = 0;
			CTRLLR_ER_CODE_2_ENG.6 := CTRLLR_EV_LOCAL[37].ONSET_STS = 1 AND CTRLLR_EV_LOCAL[37].CLEAR_STS = 0;
//			Trigger device events	
			FOR i := 0 TO DEVICE_MI_LOCAL.NUM_SIGNS_CONTROLLER - 1 DO
				DEVICES_MON_MI[i].ER_CODE_1.0 := DEVICE_EV_LOCAL[i].DEVICE_EV[0].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[0].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.1 := DEVICE_EV_LOCAL[i].DEVICE_EV[1].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[1].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.2 := DEVICE_EV_LOCAL[i].DEVICE_EV[2].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[2].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.3 := DEVICE_EV_LOCAL[i].DEVICE_EV[3].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[3].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.4 := DEVICE_EV_LOCAL[i].DEVICE_EV[4].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[4].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.5 := DEVICE_EV_LOCAL[i].DEVICE_EV[5].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[5].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.6 := DEVICE_EV_LOCAL[i].DEVICE_EV[6].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[6].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.7 := DEVICE_EV_LOCAL[i].DEVICE_EV[7].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[7].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.8 := DEVICE_EV_LOCAL[i].DEVICE_EV[8].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[8].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.9 := DEVICE_EV_LOCAL[i].DEVICE_EV[9].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[9].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.10 := DEVICE_EV_LOCAL[i].DEVICE_EV[10].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[10].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.11 := DEVICE_EV_LOCAL[i].DEVICE_EV[11].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[11].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.12 := DEVICE_EV_LOCAL[i].DEVICE_EV[12].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[12].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.13 := DEVICE_EV_LOCAL[i].DEVICE_EV[13].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[13].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.14 := DEVICE_EV_LOCAL[i].DEVICE_EV[14].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[14].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.15 := DEVICE_EV_LOCAL[i].DEVICE_EV[15].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[15].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.16 := DEVICE_EV_LOCAL[i].DEVICE_EV[16].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[16].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.17 := DEVICE_EV_LOCAL[i].DEVICE_EV[17].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[17].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.18 := DEVICE_EV_LOCAL[i].DEVICE_EV[18].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[18].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.19 := DEVICE_EV_LOCAL[i].DEVICE_EV[19].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[19].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.20 := DEVICE_EV_LOCAL[i].DEVICE_EV[20].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[20].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.21 := DEVICE_EV_LOCAL[i].DEVICE_EV[21].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[21].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.22 := DEVICE_EV_LOCAL[i].DEVICE_EV[22].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[22].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.23 := DEVICE_EV_LOCAL[i].DEVICE_EV[23].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[23].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.24 := DEVICE_EV_LOCAL[i].DEVICE_EV[24].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[24].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.25 := DEVICE_EV_LOCAL[i].DEVICE_EV[25].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[25].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.26 := DEVICE_EV_LOCAL[i].DEVICE_EV[26].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[26].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.27 := DEVICE_EV_LOCAL[i].DEVICE_EV[27].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[27].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.28 := DEVICE_EV_LOCAL[i].DEVICE_EV[28].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[28].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.29 := DEVICE_EV_LOCAL[i].DEVICE_EV[29].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[29].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_1.30 := DEVICE_EV_LOCAL[i].DEVICE_EV[30].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[30].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_2.0 := DEVICE_EV_LOCAL[i].DEVICE_EV[31].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[31].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_2.1 := DEVICE_EV_LOCAL[i].DEVICE_EV[32].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[32].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_2.2 := DEVICE_EV_LOCAL[i].DEVICE_EV[33].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[33].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_2.3 := DEVICE_EV_LOCAL[i].DEVICE_EV[34].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[34].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_2.4 := DEVICE_EV_LOCAL[i].DEVICE_EV[35].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[35].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_2.5 := DEVICE_EV_LOCAL[i].DEVICE_EV[36].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[36].CLEAR_STS = 0;
				DEVICES_MON_MI[i].ER_CODE_2.6 := DEVICE_EV_LOCAL[i].DEVICE_EV[37].ONSET_STS = 1 AND DEVICE_EV_LOCAL[i].DEVICE_EV[37].CLEAR_STS = 0;	
			END_FOR;			
			FILTER_ONS := 0;	
//			Session status
			IF CTRLLR_ER_CODE_1_ENG.1 THEN
				DEVICE_STS.5 := 0;															// RESET VALUE			
			END_IF;	
//			Event trigger by the traffic controller, but it is for device 
			IF CTRLLR_ER_CODE_1_ENG.20 OR CTRLLR_ER_CODE_1_ENG.21 OR CTRLLR_ER_CODE_2_ENG.0 THEN
				FOR i := 0 TO DEVICE_MI_LOCAL.NUM_SIGNS_CONTROLLER - 1 DO
//					Powered off by command ( 0 : False / 1 : True ) 			
					IF CTRLLR_ER_CODE_1_ENG.20 THEN 
						DEVICES_MON_MI[i].ER_CODE_1.20 := 1;
					END_IF;
//					Facility Switch override ( 0 : False / 1 : True ) 			
					IF CTRLLR_ER_CODE_1_ENG.21 THEN 
						DEVICES_MON_MI[i].ER_CODE_1.21 := 1;
					END_IF;
//					Under local control ( 0 : False / 1 : True ) 			
					IF CTRLLR_ER_CODE_2_ENG.0 THEN 
						DEVICES_MON_MI[i].ER_CODE_2.0 := 1;
					END_IF;
				END_FOR;
			END_IF;
		END_IF;
	END_IF;
//	Cycle pisition
	IF DEVICE_STS.5 THEN
		CYCLE_POS_CHR_1 := PACKET_FDBK_MI.DATA[3];									// SET VALUE
		CYCLE_POS_CHR_2 := PACKET_FDBK_MI.DATA[4];									// SET VALUE
	ELSE
		CYCLE_POS_CHR_1 := 48;														// RESET VALUE
		CYCLE_POS_CHR_2 := 48;														// RESET VALUE
	END_IF;

//	STEP 7 CONDITIONS
//	Conditions triggers
	STEP_COND[CURR_STEP].0 := PACKET_FDBK_MI.DATA[0] <> 1;							// PACKET INCORRECTED
	STEP_COND[CURR_STEP].1 := PACKET_FDBK_MI.DATA[8] = MI_CODE_CFG[0].CHR_1 AND 
	   						  PACKET_FDBK_MI.DATA[9] = MI_CODE_CFG[0].CHR_2;		// INSTRUCTION REJECTED
	STEP_COND[CURR_STEP].2 := PACKET_FDBK_MI.DATA[8] = MI_CODE_CFG[1].CHR_1 AND 
	   						  PACKET_FDBK_MI.DATA[9] = MI_CODE_CFG[1].CHR_2;		// PACKET ACKNOWLEDGE
	STEP_COND[CURR_STEP].3 := PACKET_FDBK_MI.DATA[8] = MI_CODE_CFG[3].CHR_1 AND 
	   						  PACKET_FDBK_MI.DATA[9] = MI_CODE_CFG[3].CHR_2;		// PASSWORD SEED
	STEP_COND[CURR_STEP].4 := PACKET_FDBK_MI.DATA[8] = MI_CODE_CFG[6].CHR_1 AND 
	   						  PACKET_FDBK_MI.DATA[9] = MI_CODE_CFG[6].CHR_2;		// SIGN STATUS REPLAY
	STEP_COND[CURR_STEP].5 := NOT DEVICE_STS.29;									// NO COMMS
	STEP_COND[CURR_STEP].6 := PACKET_FDBK_MI.DATA[8] = MI_CODE_CFG[21].CHR_1 AND 
	   						  PACKET_FDBK_MI.DATA[9] = MI_CODE_CFG[21].CHR_2;		// FAULT LOG REPLAY
	STEP_COND[CURR_STEP].7 := PACKET_FDBK_MI.DATA[8] = MI_CODE_CFG[14].CHR_1 AND 
	   						  PACKET_FDBK_MI.DATA[9] = MI_CODE_CFG[14].CHR_2;		// REQUEST STORED MESSAGE REPLAY
							  
		
//	STEP 7 TRANSITIONS		
//	Transitions triggers
	STEP_TRANS[CURR_STEP].0 := (STEP_COND[CURR_STEP].0 OR 	
							   STEP_COND[CURR_STEP].1) OR 	
							   (STEP_COND[CURR_STEP].2 OR 	
							   STEP_COND[CURR_STEP].3) OR 	
							   (STEP_COND[CURR_STEP].4 OR 	
							   STEP_COND[CURR_STEP].5) OR 	
							   (STEP_COND[CURR_STEP].6 OR 	
							   STEP_COND[CURR_STEP].7);
//	Transition ( DATA EXTRACTION to DATA ANALYSES )	
	IF STEP_TRANS[CURR_STEP].0 THEN
		CURR_STEP := 0;																// SET VALUE
	END_IF;
	
END_IF;		

//	EVENTS TRIGGER
//	System suppresion conditions to event viewer suppression 
	SUPRR_CON := (DEVICE_STS.0 OR DEVICE_STS.1) AND DEVICE_STS.29 AND NOT EX_SPRR_MI;
//	Controller control mode ( 0 : Remote / 1 : Local / 2 :  Local - Facility Switch )
	IF CTRLLR_ER_CODE_2_ENG.0 THEN
		FOR i := 0 TO 11 DO 
			DEVICES_MON_MI[i].CTRL_MODE := 1;			// SET TO LOCAL MODE
		END_FOR;
	ELSIF CTRLLR_ER_CODE_1_ENG.21 THEN
		FOR i := 0 TO 11 DO 
			DEVICES_MON_MI[i].CTRL_MODE := 2;			// SET TO LOCAL - FACILITY SWITCH MODE
		END_FOR;
	ELSIF NOT CTRLLR_ER_CODE_2_ENG.0 AND NOT CTRLLR_ER_CODE_1_ENG.21 THEN
		FOR i := 0 TO 11 DO 
			DEVICES_MON_MI[i].CTRL_MODE := 0;			// SET TO REMOTE MOD
		END_FOR;
	END_IF;
	
//	FAULT
//	Power failure ( 0 : False / 1 : True )
	FAULT_EVENT_FP.0 := CTRLLR_ER_CODE_1_ENG.0;		
	FAULT_EVENT_EV.0 := SUPRR_CON AND FAULT_EVENT_FP.0;
//	Host communications failure ( 0 : False / 1 : True )
	FAULT_EVENT_FP.1 := CTRLLR_ER_CODE_1_ENG.1;		
	FAULT_EVENT_EV.1 := SUPRR_CON AND FAULT_EVENT_FP.1;
//	Memory error ( 0 : False / 1 : True )
	FAULT_EVENT_FP.2 := CTRLLR_ER_CODE_1_ENG.2;
	FAULT_EVENT_EV.2 := SUPRR_CON AND FAULT_EVENT_FP.2;
//	Battery failure ( 0 : False / 1 : True )
	FAULT_EVENT_FP.3 := CTRLLR_ER_CODE_1_ENG.3;
	FAULT_EVENT_EV.3 := SUPRR_CON AND FAULT_EVENT_FP.3;
//	Communications failure ( 0 : False / 1 : True )
	FAULT_EVENT_FP.4 := NOT DEVICE_STS.29;
	FAULT_EVENT_EV.4 := (DEVICE_STS.0 OR DEVICE_STS.1) AND NOT EX_SPRR_MI AND FAULT_EVENT_FP.4;
//	Controller reset ( 0 : False / 1 : True )
	FAULT_EVENT_FP.12 :=CTRLLR_ER_CODE_1_ENG.12;
	FAULT_EVENT_EV.12 := SUPRR_CON AND FAULT_EVENT_FP.12;
//	Battery low ( 0 : False / 1 : True )
	FAULT_EVENT_FP.13 :=CTRLLR_ER_CODE_1_ENG.13;
	FAULT_EVENT_EV.13 := SUPRR_CON AND FAULT_EVENT_FP.13;
//	Main processor communications error ( 0 : False / 1 : True )
	FAULT_EVENT_FP.16 :=CTRLLR_ER_CODE_1_ENG.16;
	FAULT_EVENT_EV.16 := SUPRR_CON AND FAULT_EVENT_FP.16;
//	Mimic state error ( 0 : False / 1 : True )
	FAULT_EVENT_FP.17 :=CTRLLR_ER_CODE_1_ENG.17;
	FAULT_EVENT_EV.17 := SUPRR_CON AND FAULT_EVENT_FP.17;
//	Battery charger/regulator fault ( 0 : False / 1 : True )
	FAULT_EVENT_FP.18 :=CTRLLR_ER_CODE_1_ENG.18;
	FAULT_EVENT_EV.18 := SUPRR_CON AND FAULT_EVENT_FP.18;
	
//	ALARM
//	Equipment over-temperature ( 0 : False / 1 : True )
	ALARM_EVENT_FP.3 := CTRLLR_ER_CODE_1_ENG.23;
	ALARM_EVENT_EV.3 := SUPRR_CON AND ALARM_EVENT_FP.3;
//	Display time-out error ( 0 : False / 1 : True )
	ALARM_EVENT_FP.8 := CTRLLR_ER_CODE_1_ENG.28;
	ALARM_EVENT_EV.8 := SUPRR_CON AND ALARM_EVENT_FP.8;
//	Backup controller unavailable ( 0 : False / 1 : True )
	ALARM_EVENT_FP.9 := CTRLLR_ER_CODE_1_ENG.29;
	ALARM_EVENT_EV.9 := SUPRR_CON AND ALARM_EVENT_FP.9;
//	Cabinet door open ( 0 : False / 1 : True )
	ALARM_EVENT_FP.13 := CTRLLR_ER_CODE_2_ENG.2;
	ALARM_EVENT_EV.13 := SUPRR_CON AND ALARM_EVENT_FP.13;
//	Operating on secondary power ( 0 : False / 1 : True )
	ALARM_EVENT_FP.17 := CTRLLR_ER_CODE_2_ENG.6;
	ALARM_EVENT_EV.17 := SUPRR_CON AND ALARM_EVENT_FP.17;
//	Transaction from traffic controller corrupted ( 0 : Off / 1 : On ) 
	IF (SYS_MODE_MI = 0 AND NOT VER_PACKET_STS) OR 
	   (SYS_MODE_MI <> 0 AND VER_PACKET_ENG) THEN  
		ALARM_EVENT_FP.29 := 1;
	ELSIF (SYS_MODE_MI = 0 AND VER_PACKET_STS) OR 
		  (SYS_MODE_MI <> 0 AND NOT VER_PACKET_ENG) THEN
		ALARM_EVENT_FP.29 := 0;
	END_IF;
	ALARM_EVENT_EV.29 := SUPRR_CON AND ALARM_EVENT_FP.29;
//	Ethernet port connection attempt failure ( 0 : Off / 1 : On )
	IF TCD_MDL_STS_MI.9 THEN 
		ALARM_EVENT_FP.30 := 1;
	ELSIF NOT TCD_MDL_STS_MI.9 AND DEVICE_CMD.7 THEN 
		ALARM_EVENT_FP.30 := 0;
	END_IF;
	ALARM_EVENT_EV.30 := SUPRR_CON AND ALARM_EVENT_FP.30;

//	HEALTH STATUS
	DEVICE_STS.3 := (FAULT_EVENT_FP AND HEALTH_MASK) = 0;							

//	RESET COMMANDS ( FROM SCADA )
	DEVICE_CMD := 0;												// RESET CMD