//      __ __    __  __      ____      __  __                
//     / // /   / / / /___  / / /_____/ / / /___  _________ 
//    / // /   / /_/ // _ \/ / // ___/ /_/ // _ \/ ___/ __ \ 
//   / // /   / __  //  __/ / /(__  ) __  //  __/ /  / /_/ / 
//  /_//_/   /_/ /_/ \___/_/_//____/_/ /_/ \___/_/   \____/

$Server::preventSuicide = "delay"; //options, none = disabled suicide prevention, timeout = suicide time out after damage, delay = suicide delay, off = suicide off
$Server::preventSuicide::time = 10; //in seconds

package script_preventSuicide
{
    function serverCmdSuicide(%client)
    {
        %pl = %client.player;
        if(isObject(%pl))
        {
            if(isObject(%client.minigame) && $Server::preventSuicide !$= "none")
            {
                if($Server::preventSuicide $= "timeout")
                {
                    if(isEventPending(%pl.suicidePrevention))
                        return;
                    else
                        parent::serverCmdSuicide(%client);
                }
                else if($Server::preventSuicide $= "delay")
                {
                    if(!%pl.canSuicide)
                    {
                        %pl.suicideSequence(0);
                        return;
                    }
                    else
                        parent::serverCmdSuicide(%client);
                }
                else
                    return;
            }
            parent::serverCmdSuicide(%client);
        }
    }
    
    function Armor::damage(%this, %obj, %sourceObject, %position, %damage, %damageType)
	{
	    if(isObject(%obj.client.minigame) && $Server::preventSuicide !$= "none")
	    {
	        if($Server::preventSuicide $= "timeout")
	            %obj.suicidePrevention();
            else if($Server::preventSuicide $= "delay" && isEventPending(%obj.suicideDelay))
                %obj.suicideSequence(1);
	    }
	    Parent::damage(%this, %obj, %sourceObject, %position, %damage, %damageType);
	}
};
activatePackage(script_preventSuicide);

function Player::suicidePrevention(%this) //$server::preventSuicide = 1
{
    if(isEventPending(%this.suicidePrevention))
		cancel(%this.suicidePrevention);
    %this.suicidePrevention = %this.schedule($Server::preventSuicide::time*1000,0);
}

function Player::suicideSequence(%this,%cancel) //$server::preventSuicide = 2
{
    if(isEventPending(%this.suicideDelay))
        cancel(%this.suicideDelay);
    
    %client = %this.client;
    if(%cancel)
    {
        messageClient(%client,'',"\c6Suicide canceled!");
        %this.suicidePos = "";
        %this.suicideSequence = "";
        return;
    }
    
    if(%this.suicideSequence $= "" || %this.suicideSequence == 0)
    {
        %this.suicideSequence = 0;
        %this.suicidePos = %this.getPosition();
    }
    
    if(vectorDist(%this.getPosition(),%this.suicidePos) != 0)
    {
        messageClient(%client,'',"\c6Suicide canceled!");
        %this.suicidePos = "";
        %this.suicideSequence = "";
        return;
    }
    
    if(%this.suicideSequence == $Server::preventSuicide::time)
    {
        %this.suicideSequence = "";
        %this.suicidePos = "";
        %this.canSuicide = 1;
        call(serverCmdSuicide(%client));
        return;
    }
    %timeLeft = $Server::preventSuicide::time-%this.suicideSequence;
    messageClient(%client,'',"\c6Suicide in " @ %timeleft SPC ((%timeLeft == 1) ? "second" : "seconds"));
    %this.suicideSequence++;
    
    %this.suicideDelay = %this.schedule(1000,suicideSequence,%cancel);
}

function serverCmdSuicideOption(%client,%a,%b)
{
    if(!%client.isAdmin)
        return;
    if(%a $= "" && %b $= "")
    {
        messageClient(%client,'',"\c6Command: \c3/suicideOption A B\c6 - Current setting is \c3" @ $Server::preventSuicide @ " \c6with \c3" @ $Server::preventSuicide::time @ "\c6 " @ (($Server::preventSuicide::time == 1) ? "second" : "seconds"));
        messageClient(%client,'',"\c3A \c6can be replaced with \c3none\c6, \c3timeout\c6, \c3delay\c6, \c3off\c6 or a \c3number\c6 which represents seconds");
        messageClient(%client,'',"\c6Replace \c3A\c6 with \c3help\c6 and use one of the above options for \c3B\c6 for an explaination of what it is");
    }
    else if(%a $= "help")
    {
        switch$(%b)
        {
            case none:
                messageClient(%client,'',"\c3None \c6disables the suicide prevention script completely");
            case timeout:
                messageClient(%client,'',"\c3Timeout \c6disables suicide for a certain time period after being damaged");
            case delay:
                messageClient(%client,'',"\c3Delay \c6gives a buffer time before suiciding");
            case off:
                messageClient(%client,'',"\c3Off \c6completely disables suiciding in a mini-game");
            case number:
                messageClient(%client,'',"\c6Typing \c3/suicideOption #\c6 will make any delays or buffer times equal to this number");
            default:
                messageClient(%client,'',"\c3B \c6can be replaced with \c3none\c6, \c3timeout\c6, \c3delay\c6, \c3off\c6 or \c3number");
        }
    }
    else if(%a $= "none" || %a $= "timeout" || %a $= "delay" || %a $= "off")
    {
        messageClient(%client,'',"\c6Suicide prevention is now set to" SPC %a);
        $Server::preventSuicide = %a;
    }
    else if(%a > 0)
        messageClient(%client,'',"\c6Suicide buffer/delay time set to\c3 " @ ($Server::preventSuicide::time = %a));
}